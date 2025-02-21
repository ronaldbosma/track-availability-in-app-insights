//=============================================================================
// API Management
//=============================================================================

//=============================================================================
// Imports
//=============================================================================

import * as helpers from '../../functions/helpers.bicep'
import { apiManagementSettingsType } from '../../types/settings.bicep'

//=============================================================================
// Parameters
//=============================================================================

@description('Location to use for all resources')
param location string

@description('The tags to associate with the resource')
param tags object

@description('The settings for the API Management Service that will be created')
param apiManagementSettings apiManagementSettingsType

@description('The name of the App Insights instance that will be used by API Management')
param appInsightsName string

@description('The name of the Key Vault that will contain the secrets')
param keyVaultName string

//=============================================================================
// Variables
//=============================================================================

var serviceTags = union(tags, {
  'azd-service-name': 'apim'
})

//=============================================================================
// Existing resources
//=============================================================================

resource appInsights 'Microsoft.Insights/components@2020-02-02' existing = {
  name: appInsightsName
}

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: keyVaultName
}

resource masterSubscription 'Microsoft.ApiManagement/service/subscriptions@2023-09-01-preview' existing = {
  name: 'master'
  parent: apiManagementService
}

//=============================================================================
// Resources
//=============================================================================

// API Management - Consumption tier (see also: https://learn.microsoft.com/en-us/azure/api-management/quickstart-bicep?tabs=CLI)

resource apiManagementService 'Microsoft.ApiManagement/service@2023-09-01-preview' = {
  name: apiManagementSettings.serviceName
  location: location
  tags: serviceTags
  sku: {
    name: 'Consumption'
    capacity: 0
  }
  properties: {
    publisherName: apiManagementSettings.publisherName
    publisherEmail: apiManagementSettings.publisherEmail
  }
  identity: {
    type: 'SystemAssigned'
  }
}

// Assign roles to system-assigned identity of API Management

module assignRolesToApimSystemAssignedIdentity '../shared/assign-roles-to-principal.bicep' = {
  name: 'assignRolesToApimSystemAssignedIdentity'
  params: {
    principalId: apiManagementService.identity.principalId
    keyVaultName: keyVaultName
  }
}


// Store the app insights connection string in a named value

resource appInsightsConnectionStringNamedValue 'Microsoft.ApiManagement/service/namedValues@2023-09-01-preview' = {
  name: 'appinsights-connection-string'
  parent: apiManagementService
  properties: {
    displayName: 'appinsights-connection-string'
    value: appInsights.properties.ConnectionString
  }
}


// Configure API Management to log to App Insights
// - we need a logger that is connected to the App Insights instance
// - we need diagnostics settings that specify what to log to the logger

resource apimAppInsightsLogger 'Microsoft.ApiManagement/service/loggers@2023-09-01-preview' = {
  name: appInsightsName
  parent: apiManagementService
  properties: {
    loggerType: 'applicationInsights'
    credentials: {
      // If we would reference the connection string directly using appInsights.properties.ConnectionString,
      // a new named value is created every time we execute a deployment
      connectionString: '{{${appInsightsConnectionStringNamedValue.properties.displayName}}}'
    }
    resourceId: appInsights.id
  }
}

resource apimInsightsDiagnostics 'Microsoft.ApiManagement/service/diagnostics@2023-09-01-preview' = {
  name: 'applicationinsights' // The name of the diagnostics resource has to be applicationinsights, because that's the logger type we chose
  parent: apiManagementService
  properties: {
    alwaysLog: 'allErrors'
    loggerId: apimAppInsightsLogger.id
    httpCorrelationProtocol: 'W3C' // Enable logging to app insights in W3C format
  }
}


// Store master subscription key in Key Vault

resource apimMasterSubscriptionKeySecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  name: 'apim-master-subscription-key'
  parent: keyVault
  properties: {
    value: masterSubscription.listSecrets(apiManagementService.apiVersion).primaryKey
  }
}
