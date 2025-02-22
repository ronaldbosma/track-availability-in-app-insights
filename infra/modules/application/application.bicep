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

//=============================================================================
// Resources
//=============================================================================

module backendApi 'backend-api/backend-api.bicep' = {
  name: 'backendApi'
  params: {
    apiManagementServiceName: apiManagementSettings.serviceName
  }
}
