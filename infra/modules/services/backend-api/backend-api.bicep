//=============================================================================
// Backend API in API Management
// Including subscriptions for the Function App and Logic App
//=============================================================================

//=============================================================================
// Parameters
//=============================================================================

@description('The name of the API Management service')
param apiManagementServiceName string

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

// Backend API

resource backendApi 'Microsoft.ApiManagement/service/apis@2023-09-01-preview' = {
  name: 'backend-api'
  parent: apiManagementService
  properties: {
    displayName: 'Backend API'
    path: 'backend'
    protocols: [ 
      'https' 
    ]
    subscriptionRequired: false // Disable required subscription key for the webtest
  }

  // Create a GET Backend Status operation
  resource operations 'operations' = {
    name: 'get-backend-status'
    properties: {
      displayName: 'GET Backend Status'
      method: 'GET'
      urlTemplate: '/status'
    }

    resource policies 'policies' = {
      name: 'policy'
      properties: {
        format: 'rawxml'
        value: loadTextContent('get-status.operation.xml')
      }
    }
  }
}


// Function App Subscription

resource functionAppSubscription 'Microsoft.ApiManagement/service/subscriptions@2023-09-01-preview' = {
  parent: apiManagementService
  name: 'function-app'
  properties: {
    displayName: 'Function App Subscription'
    scope: '/apis/${backendApi.id}'
    state: 'active'
  }
}

resource functionAppSubscriptionKeySecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  name: 'function-app-subscription-key'
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
    scope: '/apis/${backendApi.id}'
    state: 'active'
  }
}

resource logicAppSubscriptionKeySecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  name: 'logic-app-subscription-key'
  parent: keyVault
  properties: {
    value: logicAppSubscription.listSecrets(apiManagementService.apiVersion).primaryKey
  }
}

