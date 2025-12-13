//=============================================================================
// Track Availability in Application Insights
// Source: https://github.com/ronaldbosma/track-availability-in-app-insights
//=============================================================================

targetScope = 'subscription'

//=============================================================================
// Imports
//=============================================================================

import { getResourceName, generateInstanceId } from './functions/naming-conventions.bicep'

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

//=============================================================================
// Variables
//=============================================================================

// Generate an instance ID to ensure unique resource names
var instanceId string = generateInstanceId(environmentName, location)

var resourceGroupName = getResourceName('resourceGroup', environmentName, location, instanceId)

var apiManagementSettings = {
  serviceName: getResourceName('apiManagement', environmentName, location, instanceId)
  sku: 'Consumption'
  publisherName: 'admin@example.org'
  publisherEmail: 'admin@example.org'
}

var appInsightsSettings = {
  appInsightsName: getResourceName('applicationInsights', environmentName, location, instanceId)
  logAnalyticsWorkspaceName: getResourceName('logAnalyticsWorkspace', environmentName, location, instanceId)
  retentionInDays: 30
}

var functionAppSettings = {
  functionAppName: getResourceName('functionApp', environmentName, location, instanceId)
  appServicePlanName: getResourceName('appServicePlan', environmentName, location, 'functionapp-${instanceId}')
  netFrameworkVersion: 'v9.0'
}

var logicAppSettings = {
  logicAppName: getResourceName('logicApp', environmentName, location, instanceId)
  appServicePlanName: getResourceName('appServicePlan', environmentName, location, 'logicapp-${instanceId}')
  netFrameworkVersion: 'v9.0'
}

var keyVaultName = getResourceName('keyVault', environmentName, location, instanceId)

var storageAccountName = getResourceName('storageAccount', environmentName, location, instanceId)

var tags = {
  'azd-env-name': environmentName
  'azd-template': 'ronaldbosma/track-availability-in-app-insights'
}

//=============================================================================
// Resources
//=============================================================================

resource resourceGroup 'Microsoft.Resources/resourceGroups@2024-07-01' = {
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

module availabilityTests 'modules/application/availability-tests.bicep' = {
  scope: resourceGroup
  params: {
    environmentName: environmentName
    location: location
    tags: tags
    apiManagementServiceName: apiManagementSettings.serviceName
    appInsightsName: appInsightsSettings.appInsightsName
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

// Return the approximate failure percentage
output APPROXIMATE_FAILURE_PERCENTAGE int = approximateFailurePercentage
