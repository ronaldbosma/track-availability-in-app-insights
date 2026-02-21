//=============================================================================
// Track Availability in Application Insights
// Source: https://github.com/ronaldbosma/track-availability-in-app-insights
//=============================================================================

targetScope = 'subscription'

//=============================================================================
// Imports
//=============================================================================

import { getResourceName, generateInstanceId } from './functions/naming-conventions.bicep'
import { apiManagementSettingsType, appInsightsSettingsType, functionAppSettingsType, logicAppSettingsType } from './types/settings.bicep'

//=============================================================================
// Parameters
//=============================================================================

@minLength(1)
@description('Location to use for all resources')
param location string

@minLength(1)
@maxLength(32)
@description('The name of the environment to deploy to')
param environmentName string

@description('The approximate percentage of failures that will be simulated. 0-100')
@minValue(0)
@maxValue(100)
param approximateFailurePercentage int

@description('Lifespan of SSL certificate validity in days. SSL certificate check availability test will fail if the certificate expires within this number of days.')
@allowed([1, 7, 30, 90, 365])
param sslCertRemainingLifetimeDays int

@description('Email address to send alerts to')
param alertRecipientEmailAddress string = ''

//=============================================================================
// Variables
//=============================================================================

// Generate an instance ID to ensure unique resource names
var instanceId string = generateInstanceId(environmentName, location)

var resourceGroupName string = getResourceName('resourceGroup', environmentName, location, instanceId)

var actionGroupName string = getResourceName('actionGroup', environmentName, location, instanceId)

var apiManagementSettings apiManagementSettingsType = {
  serviceName: getResourceName('apiManagement', environmentName, location, instanceId)
  sku: 'Consumption'
}

var appInsightsSettings appInsightsSettingsType = {
  appInsightsName: getResourceName('applicationInsights', environmentName, location, instanceId)
  logAnalyticsWorkspaceName: getResourceName('logAnalyticsWorkspace', environmentName, location, instanceId)
  retentionInDays: 30
}

var functionAppSettings functionAppSettingsType = {
  functionAppName: getResourceName('functionApp', environmentName, location, instanceId)
  appServicePlanName: getResourceName('appServicePlan', environmentName, location, 'functionapp-${instanceId}')
  netFrameworkVersion: 'v10.0'
}

var logicAppSettings logicAppSettingsType = {
  logicAppName: getResourceName('logicApp', environmentName, location, instanceId)
  appServicePlanName: getResourceName('appServicePlan', environmentName, location, 'logicapp-${instanceId}')
  netFrameworkVersion: 'v8.0'
}

var keyVaultName string = getResourceName('keyVault', environmentName, location, instanceId)

var storageAccountName string = getResourceName('storageAccount', environmentName, location, instanceId)

var tags { *: string } = {
  'azd-env-name': environmentName
  'azd-template': 'ronaldbosma/track-availability-in-app-insights'
  
  // The SecurityControl tag is added to Trainer Demo Deploy projects so resources can run in MTT managed subscriptions without being blocked by default security policies.
  // DO NOT USE this tag in production or customer subscriptions.
  SecurityControl: 'Ignore'
}

//=============================================================================
// Resources
//=============================================================================

resource resourceGroup 'Microsoft.Resources/resourceGroups@2024-11-01' = {
  name: resourceGroupName
  location: location
  tags: tags
}

module keyVault 'modules/services/key-vault.bicep' = {
  scope: resourceGroup
  params: {
    location: location
    tags: tags
    keyVaultName: keyVaultName
  }
}

module storageAccount 'modules/services/storage-account.bicep' = {
  scope: resourceGroup
  params: {
    location: location
    tags: tags
    storageAccountName: storageAccountName
  }
}

module appInsights 'modules/services/app-insights.bicep' = {
  scope: resourceGroup
  params: {
    location: location
    tags: tags
    appInsightsSettings: appInsightsSettings
  }
}

module apiManagement 'modules/services/api-management.bicep' = {
  scope: resourceGroup
  params: {
    location: location
    tags: tags
    apiManagementSettings: apiManagementSettings
    appInsightsName: appInsightsSettings.appInsightsName
    keyVaultName: keyVaultName
  }
  dependsOn: [
    appInsights
    keyVault
  ]
}

module functionApp 'modules/services/function-app.bicep' = {
  scope: resourceGroup
  params: {
    location: location
    tags: tags
    functionAppSettings: functionAppSettings
    apiManagementSettings: apiManagementSettings
    appInsightsName: appInsightsSettings.appInsightsName
    keyVaultName: keyVaultName
    storageAccountName: storageAccountName
    sslCertRemainingLifetimeDays: sslCertRemainingLifetimeDays
  }
  dependsOn: [
    appInsights
    backendApi
    keyVault
    storageAccount
  ]
}

module logicApp 'modules/services/logic-app.bicep' = {
  scope: resourceGroup
  params: {
    location: location
    tags: tags
    logicAppSettings: logicAppSettings
    apiManagementSettings: apiManagementSettings
    appInsightsName: appInsightsSettings.appInsightsName
    keyVaultName: keyVaultName
    storageAccountName: storageAccountName
    sslCertRemainingLifetimeDays: sslCertRemainingLifetimeDays
  }
  dependsOn: [
    appInsights
    backendApi
    keyVault
    storageAccount
  ]
}

module assignRolesToDeployer 'modules/shared/assign-roles-to-principal.bicep' = {
  scope: resourceGroup
  params: {
    principalId: deployer().objectId
    isAdmin: true
    appInsightsName: appInsightsSettings.appInsightsName
    keyVaultName: keyVaultName
    storageAccountName: storageAccountName
  }
  dependsOn: [
    keyVault
    storageAccount
  ]
}


//=============================================================================
// Application Resources
//=============================================================================

module backendApi 'modules/application/backend-api.bicep' = {
  scope: resourceGroup
  params: {
    apiManagementServiceName: apiManagementSettings.serviceName
    keyVaultName: keyVaultName
    approximateFailurePercentage: approximateFailurePercentage
  }
  dependsOn: [
    apiManagement
    keyVault
  ]
}

module actionGroup 'modules/application/action-group.bicep' = {
  scope: resourceGroup
  params: {
    name: actionGroupName
    alertRecipientEmailAddress: alertRecipientEmailAddress
  }
}

module availabilityTests 'modules/application/availability-tests.bicep' = {
  scope: resourceGroup
  params: {
    environmentName: environmentName
    location: location
    tags: tags
    apiManagementSettings: apiManagementSettings
    appInsightsName: appInsightsSettings.appInsightsName
    sslCertRemainingLifetimeDays: sslCertRemainingLifetimeDays
  }
  dependsOn: [
    appInsights
    apiManagement
    backendApi
  ]
}

module alerts 'modules/application/alerts.bicep' = {
  scope: resourceGroup
  params: {
    environmentName: environmentName
    location: location
    tags: tags
    appInsightsName: appInsightsSettings.appInsightsName
    actionGroupId: actionGroup.outputs.id
  }
  dependsOn: [
    appInsights
  ]
}


//=============================================================================
// Outputs
//=============================================================================

// Return the names of the resources
output AZURE_API_MANAGEMENT_NAME string = apiManagementSettings.serviceName
output AZURE_APPLICATION_INSIGHTS_NAME string = appInsightsSettings.appInsightsName
output AZURE_FUNCTION_APP_NAME string = functionAppSettings.functionAppName
output AZURE_KEY_VAULT_NAME string = keyVaultName
output AZURE_LOG_ANALYTICS_WORKSPACE_NAME string = appInsightsSettings.logAnalyticsWorkspaceName
output AZURE_LOGIC_APP_NAME string = logicAppSettings.logicAppName
output AZURE_RESOURCE_GROUP string = resourceGroupName
output AZURE_STORAGE_ACCOUNT_NAME string = storageAccountName

// Return configuration settings
output APPROXIMATE_FAILURE_PERCENTAGE int = approximateFailurePercentage
output SSL_CERT_REMAINING_LIFETIME_DAYS int = sslCertRemainingLifetimeDays
output ALERT_RECIPIENT_EMAIL_ADDRESS string = alertRecipientEmailAddress
