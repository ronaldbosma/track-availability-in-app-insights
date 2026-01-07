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

@description('The approximate percentage of failures that will be simulated. 0-100')
param approximateFailurePercentage int

//=============================================================================
// Existing resources
//=============================================================================

resource apiManagementService 'Microsoft.ApiManagement/service@2024-10-01-preview' existing = {
  name: apiManagementServiceName
}

resource keyVault 'Microsoft.KeyVault/vaults@2024-11-01' existing = {
  name: keyVaultName
}

//=============================================================================
// Resources
//=============================================================================

// Named value for approximate failure percentage

resource approximateFailurePercentageNamedValue 'Microsoft.ApiManagement/service/namedValues@2024-10-01-preview' = {
  name: 'approximate-failure-percentage'
  parent: apiManagementService
  properties: {
    displayName: 'approximate-failure-percentage'
    value: string(approximateFailurePercentage)
  }
}

// Backend API

resource backendApi 'Microsoft.ApiManagement/service/apis@2024-10-01-preview' = {
  name: 'backend-api'
  parent: apiManagementService
  properties: {
    displayName: 'Backend API'
    path: 'backend'
    protocols: [ 
      'https' 
    ]
    subscriptionRequired: false // Disable required subscription key for the standard test (webtest)
  }

  // Create a GET Status operation
  resource operations 'operations' = {
    name: 'get-status'
    properties: {
      displayName: 'Get Status of Backend'
      method: 'GET'
      urlTemplate: '/status'
    }

    resource policies 'policies' = {
      name: 'policy'
      properties: {
        format: 'rawxml'
        value: loadTextContent('backend-api.get-status.xml')
      }
    }
  }

  dependsOn: [
    approximateFailurePercentageNamedValue
  ]
}


// Function App Subscription

resource functionAppSubscription 'Microsoft.ApiManagement/service/subscriptions@2024-10-01-preview' = {
  parent: apiManagementService
  name: 'function-app'
  properties: {
    displayName: 'Function App Subscription'
    scope: '/apis/${backendApi.id}'
    state: 'active'
  }
}

resource functionAppSubscriptionKeySecret 'Microsoft.KeyVault/vaults/secrets@2024-11-01' = {
  name: 'function-app-subscription-key'
  parent: keyVault
  properties: {
    value: functionAppSubscription.listSecrets(apiManagementService.apiVersion).primaryKey
  }
}


// Logic App Subscription

resource logicAppSubscription 'Microsoft.ApiManagement/service/subscriptions@2024-10-01-preview' = {
  parent: apiManagementService
  name: 'logic-app'
  properties: {
    displayName: 'Logic App Subscription'
    scope: '/apis/${backendApi.id}'
    state: 'active'
  }
}

resource logicAppSubscriptionKeySecret 'Microsoft.KeyVault/vaults/secrets@2024-11-01' = {
  name: 'logic-app-subscription-key'
  parent: keyVault
  properties: {
    value: logicAppSubscription.listSecrets(apiManagementService.apiVersion).primaryKey
  }
}

