//=============================================================================
// Application
//=============================================================================

//=============================================================================
// Imports
//=============================================================================

import { getResourceName, getInstanceId } from '../../functions/naming-conventions.bicep'
import * as helpers from '../../functions/helpers.bicep'

//=============================================================================
// Parameters
//=============================================================================

@description('The name of the environment to deploy to')
param environmentName string

@description('Location to use for all resources')
param location string

@description('The tags to associate with the resource')
param tags object

@description('The name of the API Management service')
param apiManagementServiceName string

@description('The name of the App Insights instance that will be used by the Logic App')
param appInsightsName string

//=============================================================================
// Variables
//=============================================================================

var backendApiStatusAvailabilityTestName = getResourceName('webtest', environmentName, location, 'backend-api-status')

//=============================================================================
// Existing resources
//=============================================================================

resource appInsights 'Microsoft.Insights/components@2020-02-02' existing = {
  name: appInsightsName
}

//=============================================================================
// Resources
//=============================================================================

// Add availability test on the status endpoint of the backend API

resource backendApiStatusAvailabilityTest 'Microsoft.Insights/webtests@2022-06-15' = {
  name: backendApiStatusAvailabilityTestName
  location: location
  tags: union(tags, {
    'hidden-link:${appInsights.id}': 'Resource'
  })

  properties: {
    Name: 'Backend API Status'
    Description: 'Status of the backend API'

    Kind: 'standard'
    Enabled: true
    RetryEnabled: false // Set to false for this demo to reduce the number of failed calls

    // A frequence of 300 means that every 5 minutes the test will execute from all configured locations.
    // So if you have 5 locations, the test will run 5 times every 5 minutes.
    // Note that the test will not run exactly every minute.
    Frequency: 300

    Request: {
      HttpVerb: 'GET'
      RequestUrl: '${helpers.getApiManagementGatewayUrl(apiManagementServiceName)}/backend/status'
    }

    ValidationRules: {
      ExpectedHttpStatusCode: 200
      IgnoreHttpStatusCode: false
      SSLCheck: false
    }

    // For a list of available locations, see: https://learn.microsoft.com/en-us/previous-versions/azure/azure-monitor/app/monitor-web-app-availability#location-population-tags
    Locations: [
      {
        Id: 'emea-nl-ams-azr' // West Europe
      }
      {
        Id: 'emea-gb-db3-azr' // North Europe
      }
      {
        Id: 'emea-ru-msa-edge' // UK South
      }
      {
        Id: 'emea-fr-pra-edge' // France Central
      }
      {
        Id: 'emea-ch-zrh-edge' // France South
      }
    ]

    SyntheticMonitorId: backendApiStatusAvailabilityTestName
  }
}
