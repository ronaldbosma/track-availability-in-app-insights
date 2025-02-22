//=============================================================================
// Subscriptions in API Management for the Function & Logic App
// The primary key for each subscription is stored in Key Vault
//=============================================================================

//=============================================================================
// Parameters
//=============================================================================

@description('The name of the API Management service')
param apiManagementServiceName string

@description('The ID of the backend API for which to create the subscriptions')
param backendApiId string

@description('The name of the Key Vault that will contain the secrets')
param keyVaultName string

//=============================================================================
// Existing resources
//=============================================================================

resource apiManagementService 'Microsoft.ApiManagement/service@2023-09-01-preview' existing = {
  name: apiManagementServiceName
}

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: keyVaultName
}

//=============================================================================
// Resources
//=============================================================================

// Function App Subscription

resource functionAppSubscription 'Microsoft.ApiManagement/service/subscriptions@2023-09-01-preview' = {
  parent: apiManagementService
  name: 'function-app'
  properties: {
    displayName: 'Function App Subscription'
    scope: '/apis/${backendApiId}'
    state: 'active'
  }
}

resource functionAppSubscriptionKeySecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  name: 'functionApp-subscription-key'
  parent: keyVault
  properties: {
    value: functionAppSubscription.listSecrets(apiManagementService.apiVersion).primaryKey
  }
}


// Logic App Subscription

resource logicAppSubscription 'Microsoft.ApiManagement/service/subscriptions@2023-09-01-preview' = {
  parent: apiManagementService
  name: 'logic-app'
  properties: {
    displayName: 'Logic App Subscription'
    scope: '/apis/${backendApiId}'
    state: 'active'
  }
}

resource logicAppSubscriptionKeySecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  name: 'logicApp-subscription-key'
  parent: keyVault
  properties: {
    value: logicAppSubscription.listSecrets(apiManagementService.apiVersion).primaryKey
  }
}
