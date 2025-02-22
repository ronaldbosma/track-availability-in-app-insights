//=============================================================================
// Alerts
//=============================================================================

//=============================================================================
// Imports
//=============================================================================

import { getResourceName } from '../../functions/naming-conventions.bicep'

//=============================================================================
// Parameters
//=============================================================================

@description('The name of the environment to deploy to')
param environmentName string

@description('Location to use for all resources')
param location string

@description('The tags to associate with the resource')
param tags object

@description('The name of the App Insights instance that will be used by the Logic App')
param appInsightsName string

//=============================================================================
// Existing resources
//=============================================================================

resource appInsights 'Microsoft.Insights/components@2020-02-02' existing = {
  name: appInsightsName
}

//=============================================================================
// Resources
//=============================================================================

// Create alert rule that triggers on failing availability tests

resource failedAvailabilityTestAlert 'Microsoft.Insights/metricAlerts@2018-03-01' = {
  name: getResourceName('alert', environmentName, location, 'failed-availability-test')
  location: 'global'
  tags: tags

  properties: {
    description: 'Alert that triggers when an availability test fails'
    severity: 1
    enabled: true
    autoMitigate: true

    scopes: [
      appInsights.id
    ]

    evaluationFrequency: 'PT5M' // Execute every 5 minutes
    windowSize: 'PT5M'          // Look at the availability test results from the last 5 minutes

    criteria: {
      allOf: [
        {
          // Available metrics can be found here: https://learn.microsoft.com/en-us/azure/azure-monitor/app/metrics-overview?tabs=standard#available-metrics
          name: 'AvailabilityMetric'
          metricNamespace: 'microsoft.insights/components'
          metricName: 'availabilityResults/availabilityPercentage'

          // Alert triggers if the average availability percentage is less than 100%
          timeAggregation: 'Average'
          operator: 'LessThan'
          threshold: 100
          
          // This dimension is used to split the alerts on the name of the availability test so you get notified of each failing test separately.
          // Available dimensions can be found here: https://learn.microsoft.com/en-us/azure/azure-monitor/reference/supported-metrics/microsoft-insights-components-metrics#category-availability
          dimensions: [
            {
              name: 'availabilityResult/name'
              operator: 'Include'
              values: [
                '*'
              ]
            }
          ]
          
          skipMetricValidation: false
          criterionType: 'StaticThresholdCriterion'
        }
      ]
      'odata.type': 'Microsoft.Azure.Monitor.SingleResourceMultipleMetricCriteria'
    }
  }
}
