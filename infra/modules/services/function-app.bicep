//=============================================================================
// Function App
//=============================================================================

//=============================================================================
// Imports
//=============================================================================

import * as helpers from '../../functions/helpers.bicep'
import { apiManagementSettingsType, functionAppSettingsType } from '../../types/settings.bicep'
import { tagsType } from '../../types/shared-types.bicep'

//=============================================================================
// Parameters
//=============================================================================

@description('Location to use for all resources')
param location string

@description('The tags to associate with the resource')
param tags tagsType

@description('The settings for the Function App that will be created')
param functionAppSettings functionAppSettingsType

@description('The settings for the API Management Service')
param apiManagementSettings apiManagementSettingsType

@description('The name of the App Insights instance that will be used by the Function App')
param appInsightsName string

@description('The name of the Key Vault that will contain the secrets')
param keyVaultName string

@description('Name of the storage account that will be used by the Function App')
param storageAccountName string

@description('Lifespan of SSL certificate validity in days. SSL certificate check availability test will fail if the certificate expires within this number of days.')
@minValue(0)
param sslCertRemainingLifetimeDays int

//=============================================================================
// Variables
//=============================================================================

// azd uses the 'azd-service-name' tag to identify the service when deploying the app source code from the src folder.
// In this case the assemblies of the functions .NET solution.
var serviceTags { *: string } = union(tags, {
  'azd-service-name': 'functionApp'
})

var appSettings resourceInput<'Microsoft.Web/sites/config@2025-03-01'>.properties = {
  APPLICATIONINSIGHTS_AUTHENTICATION_STRING: 'Authorization=AAD'
  APPLICATIONINSIGHTS_CONNECTION_STRING: appInsights.properties.ConnectionString

  AzureWebJobsStorage__credential: 'managedidentity'
  AzureWebJobsStorage__blobServiceUri: helpers.getBlobStorageEndpoint(storageAccountName)
  AzureWebJobsStorage__queueServiceUri: helpers.getQueueStorageEndpoint(storageAccountName)
  AzureWebJobsStorage__tableServiceUri: helpers.getTableStorageEndpoint(storageAccountName)

  // API Management App Settings
  ApiManagement__GatewayUrl: helpers.getApiManagementGatewayUrl(apiManagementSettings.serviceName)
  ApiManagement__SubscriptionKey: helpers.getKeyVaultSecretReference(keyVaultName, 'function-app-subscription-key')
  ApiManagement__StatusEndpoint: helpers.getApiManagementStatusEndpoint(apiManagementSettings.sku)

  // Availability test settings
  AVAILABILITY_TESTS_SCHEDULE: '0 * * * * *' // Every minute
  SSL_CERT_REMAINING_LIFETIME_DAYS: string(sslCertRemainingLifetimeDays)
}

//=============================================================================
// Existing resources
//=============================================================================

resource appInsights 'Microsoft.Insights/components@2020-02-02' existing = {
  name: appInsightsName
}

resource storageAccount 'Microsoft.Storage/storageAccounts@2026-09-01' existing = {
  name: storageAccountName
}

resource storageAccountBlobServices 'Microsoft.Storage/storageAccounts/blobServices@2026-09-01' existing = {
  parent: storageAccount
  name: 'default'
}

//=============================================================================
// Resources
//=============================================================================

// Create the App Service Plan for the Function App

resource hostingPlan 'Microsoft.Web/serverfarms@2025-03-01' = {
  name: functionAppSettings.appServicePlanName
  location: location
  tags: tags
  kind: 'functionapp'
  sku: {
    name: 'FC1'
    tier: 'FlexConsumption'
  }
  properties: {
    reserved: true
  }
}

// Create the storage container for the Function App package

resource appPackageStorageContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2026-09-01' = {
  parent: storageAccountBlobServices
  name: 'app-package-${functionAppSettings.functionAppName}'
}

// Create the Function App

resource functionApp 'Microsoft.Web/sites@2025-03-01' = {
  name: functionAppSettings.functionAppName
  location: location
  tags: serviceTags
  kind: 'functionapp,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: hostingPlan.id
    siteConfig: {
      // NOTE: the app settings will be set separately
      ftpsState: 'FtpsOnly'
      minTlsVersion: '1.2'
    }
    functionAppConfig: {
      deployment: {
        storage: {
          type: 'blobContainer'
          value: '${helpers.getBlobStorageEndpoint(storageAccountName)}/${appPackageStorageContainer.name}'
          authentication: {
            type: 'SystemAssignedIdentity'
          }
        }
      }
      runtime: {
        name: 'dotnet-isolated'
        version: functionAppSettings.runtimeVersion
      }
      scaleAndConcurrency: {
        maximumInstanceCount: 100
        instanceMemoryMB: 2048
      }
    }
    httpsOnly: true
  }
}

// Assign roles to system-assigned identity of Function App

module assignRolesToFunctionAppSystemAssignedIdentity '../shared/assign-roles-to-principal.bicep' = {
  params: {
    principalId: functionApp.identity.principalId
    principalType: 'ServicePrincipal'
    appInsightsName: appInsightsName
    keyVaultName: keyVaultName
    storageAccountName: storageAccountName
  }
}

// Set standard App Settings
//  NOTE: this is done in a separate module that merges the app settings with the existing ones
//        to prevent other (manually) created app settings from being removed.

module setFunctionAppSettings '../shared/merge-app-settings.bicep' = {
  params: {
    siteName: functionAppSettings.functionAppName
    currentAppSettings: list('${functionApp.id}/config/appsettings', functionApp.apiVersion).properties
    newAppSettings: appSettings
  }
  dependsOn: [
    assignRolesToFunctionAppSystemAssignedIdentity // App settings might be dependent on the function app having access to e.g. Key Vault
  ]
}
