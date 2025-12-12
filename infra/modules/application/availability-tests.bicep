//=============================================================================
// Availability Tests
//=============================================================================

//=============================================================================
// Imports
//=============================================================================

import { getResourceName } from '../../functions/naming-conventions.bicep'
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
var apimSslCertificateCheckAvailabilityTestName = getResourceName('webtest', environmentName, location, 'apim-ssl-certificate-check')

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
    Name: 'Standard Test - Backend API Status' // This name will be displayed in the availability test overview in the Azure portal
    Description: 'Status of the backend API tested from a standard test (webtest)'

    Kind: 'standard'
    Enabled: true
    RetryEnabled: false // Set to false for this demo to reduce the number of failed calls

    // A frequence of 300 means that every 5 minutes the test will execute from all configured locations.
    // So if you have 5 locations, the test will run 5 times every 5 minutes.
    // Note that the test will not run exactly every minute.
    Frequency: 300

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

    Request: {
      HttpVerb: 'GET'
      RequestUrl: '${helpers.getApiManagementGatewayUrl(apiManagementServiceName)}/backend/status'
    }

    ValidationRules: {
      ExpectedHttpStatusCode: 200
      IgnoreHttpStatusCode: false
      SSLCheck: false // A separate test will check the SSL certificate of the API Management service
    }

    SyntheticMonitorId: backendApiStatusAvailabilityTestName
  }
}


// Add availability test that checks if the SSL certificate of the API Management service is valid for at least 30 days

resource apimSslCertificateCheckAvailabilityTest 'Microsoft.Insights/webtests@2022-06-15' = {
  name: apimSslCertificateCheckAvailabilityTestName
  location: location
  tags: union(tags, {
    'hidden-link:${appInsights.id}': 'Resource'
  })

  properties: {
    Name: 'Standard Test - API Management SSL Certificate Check' // This name will be displayed in the availability test overview in the Azure portal
    Description: 'Check if the SSL certificate of the API Management service is valid for at least 30 days'

    Kind: 'standard'
    Enabled: true
    RetryEnabled: false // Set to false for this demo to reduce the number of failed calls

    // NOTE: Normally I would set the frequency to 900 (15 minutes) and execute the test from a single location
    //       because we don't have to be notified the instant that the certificate expires within 30 days and it minimizes costs.
    //
    //       However, for demo purposes I set the frequency to 300 (5 minutes) and configured multiple locations.
    //       This also makes the 'Verify Availability Tests' step in the GitHub Actions workflow succeed faster.
    Frequency: 300
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

    // Get status of API Management using the default status endpoint.
    // For the Consumptier tier, the status endpoint is /internal-status-0123456789abcdef. For other tiers it's /status-0123456789abcdef.
    Request: {
      HttpVerb: 'GET'
      RequestUrl: '${helpers.getApiManagementGatewayUrl(apiManagementServiceName)}/internal-status-0123456789abcdef'
    }

    ValidationRules: {
      ExpectedHttpStatusCode: 200
      IgnoreHttpStatusCode: false
      SSLCheck: true
      SSLCertRemainingLifetimeCheck: 30 // Check if the SSL certificate is valid for at least 30 days
    }

    SyntheticMonitorId: apimSslCertificateCheckAvailabilityTestName
  }
}
