//=============================================================================
// Application Resources
// These are pure Bicep and can't be deployed separately by azd yet
//=============================================================================

//=============================================================================
// Imports
//=============================================================================

import { apiManagementSettingsType } from '../../types/settings.bicep'

//=============================================================================
// Parameters
//=============================================================================

@description('The settings for the API Management Service')
param apiManagementSettings apiManagementSettingsType

@description('The name of the Key Vault that will contain the secrets')
param keyVaultName string

//=============================================================================
// Resources
//=============================================================================

module backendApi 'backend-api/backend-api.bicep' = {
  name: 'backendApi'
  params: {
    apiManagementServiceName: apiManagementSettings.serviceName
  }
}

module subscriptions 'subscriptions/subscriptions.bicep' = {
  name: 'subscriptions'
  params: {
    apiManagementServiceName: apiManagementSettings.serviceName
    backendApiId: backendApi.outputs.backendApiId
    keyVaultName: keyVaultName
  }
}
