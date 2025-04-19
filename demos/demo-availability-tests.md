# Track Availability in Application Insights - Demo Availability Tests

In this demo scenario, we will demonstrate the availability tests that are deployed as part of the template. The template deploys serveral different availability tests: standard tests (webtest), Azure Functions, and a Logic App workflow. The tests check the availability of an API in API Management and the validity of the SSL certificate of the API Management service. An alert on failing tests is also included. See the following diagram for an overview:

![Infra](https://raw.githubusercontent.com/ronaldbosma/track-availability-in-app-insights/refs/heads/main/images/track-availability-diagrams-app.png)

## 1. What resources are getting deployed

The following resources will be deployed:

![Deployed Resources](https://raw.githubusercontent.com/ronaldbosma/track-availability-in-app-insights/refs/heads/main/images/deployed-resources.png)


## 2. What can I demo from this scenario after deployment

### Availability Test Results

Show the availability test results.

1. Navigate to the Application Insights resource in the Azure portal.
1. Click the `Availability` tab in the left menu. The result should look like this:

   ![Availability Test Results](https://raw.githubusercontent.com/ronaldbosma/track-availability-in-app-insights/refs/heads/main/images/availability-test-results.png)

   Note the `CUSTOM` label on some of the tests. This indicates that the test is not a standard test executed from App Insights, but a custom test executed from a different location that publishes its results to App Insights.

#### Standard Test Result

Show details of a standard test.

1. Select the `Standard Test - Backend API Status` test.
1. Click on either `Successful` or `Failed`.
1. Click on an availability test result in the right pane to view the details.
1. Show the information in the end-to-end transaction details screen. See the following image for an example:  

   ![Standard Test - End-to-end Transaction Details](https://raw.githubusercontent.com/ronaldbosma/track-availability-in-app-insights/refs/heads/main/images/standard-test-end-to-end-transaction-details.png)

   Note that the API Mangement request logging is included.

1. Close the end-to-end transaction details.

#### Azure Function Test Result

Show details of a test executed from an Azure Function.

1. Select the `Azure Function - Backend API Status` test.
1. Click on either `Successful` or `Failed`.
1. Click on an availability test result in the right pane to view the details.
1. Show the information in the end-to-end transaction details screen. See the following image for an example:  

   ![Azure Function - End-to-end Transaction Details](https://raw.githubusercontent.com/ronaldbosma/track-availability-in-app-insights/refs/heads/main/images/azure-function-end-to-end-transaction-details.png)

   Both the logging of the Azure Function and the API Management request logging are included.

1. Close the end-to-end transaction details.

#### Logic App Workflow Test Result

Show details of a test executed from an Logic App Workflow.

1. Select the `Logic App Workflow - Backend API Status` test.
1. Click on either `Successful` or `Failed`.
1. Click on an availability test result in the right pane to view the details.
1. Show the information in the end-to-end transaction details screen. See the following image for an example:  

   ![Logic App Workflow - End-to-end Transaction Details](https://raw.githubusercontent.com/ronaldbosma/track-availability-in-app-insights/refs/heads/main/images/logic-app-workflow-end-to-end-transaction-details.png)

   Both the logging of the Logic App Workflow and the API Management request logging are included. The timeline is a little bit 'messy' compared to the others because of the way the Logic App tracks the availability result as a separate action.

1. Close the end-to-end transaction details.


### Standard Tests

Show the standard tests. They are deployed as part of the Bicep infrastructure and defined in the `availability-tests.bicep` file.

1. Open [availability-tests.bicep](https://github.com/ronaldbosma/track-availability-in-app-insights/blob/main/infra/modules/application/availability-tests.bicep).

1. Locate the `backendApiStatusAvailabilityTest` resource that deploys the `Standard Test - Backend API Status` test.  

   This test checks that the `/backend/status` endpoint in the API Management service is available.   

   Some things to note about this test:
   - The webtest is linked to Application Insights via a hidden tag.
   - The frequency is set to 300 seconds (5 minutes) and the test is executed from 5 different locations. 
     This means that the test is executed from each location every 5 minutes. Note that the tests will not run exactly every minute.
   - The retry has been disabled for demo purposes.
   - The SSL check has been disabled.

1. Locate the `apimSslCertificateCheckAvailabilityTest` resource that deploys the `Standard Test - API Management SSL Certificate Check'` test.  

   The purpose of this test is to check that the SSL certificate of API Management is valid and not close to expiring. 
   This test use the standard status endpoint of the API Management service to check for availability. 
   In this case we use `/internal-status-0123456789abcdef` because we've deployed the Consumption tier of API Management. 
   For other API Management iters you can use `/status-0123456789abcdef`  .

   Some things to note about this test:  
   - The SSL check has been enabled and the test will fail if the certificate expires within 30 days or has already expired.
   - The frequency is as high as possible (15 minutes) because we don't have to be notified the instant that the certificate expires within 30 days.
     It also runs from only one location to reduce cost.
   - The retry has been disabled for demo purposes.

1. Navigate to the Application Insights resource in the Azure portal.
1. Click the `Availability` tab in the left menu.
1. Click on the Edit button (pencil) of the `Standard Test - Backend API Status` test.
1. Note that you can add custom headers to be passed with the request under `Standard test info`. 
   **Do not use secrets here!** The values are stored in plain text in the test definition and Key Vault references are not supported.


### Azure Function



### Logic App Workflow



### Alerts

