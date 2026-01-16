# Track Availability in Application Insights - Demo Availability Tests

In this demo scenario, we will demonstrate the availability tests that are deployed as part of the template. 
The template deploys serveral different availability tests: standard tests (webtest), Azure Functions, and a Logic App workflow. 
The tests check the availability of an API in API Management and the validity of the SSL certificate of the API Management service. 
Alerts on failing tests and requests are also included. See the following diagram for an overview:

![Infra](https://raw.githubusercontent.com/ronaldbosma/track-availability-in-app-insights/refs/heads/main/images/diagrams-overview.png)

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

Show details of a test executed from a Logic App Workflow.

1. Select the `Logic App Workflow - Backend API Status` test.
1. Click on either `Successful` or `Failed`.
1. Click on an availability test result in the right pane to view the details.
1. Show the information in the end-to-end transaction details screen. See the following image for an example:  

   ![Logic App Workflow - End-to-end Transaction Details](https://raw.githubusercontent.com/ronaldbosma/track-availability-in-app-insights/refs/heads/main/images/logic-app-workflow-end-to-end-transaction-details.png)

   Both the logging of the Logic App Workflow and the API Management request logging are included. The timeline is a little bit 'messy' compared to the others because of the way the workflow tracks the availability result as a separate action.

1. Close the end-to-end transaction details.


### Standard Tests

Show the standard tests. They are deployed as part of the Bicep infrastructure and defined in the `availability-tests.bicep` file.

1. Open [availability-tests.bicep](https://github.com/ronaldbosma/track-availability-in-app-insights/blob/main/infra/modules/application/availability-tests.bicep).

1. Locate the `backendApiStatusAvailabilityTest` resource that deploys the `Standard Test - Backend API Status` test.  

   This test checks that the `/backend/status` endpoint in the API Management service is available.   

   Some things to note about this test:
   - The webtest resource is linked to Application Insights via a hidden tag.
   - The frequency is set to 300 seconds (5 minutes) and the test is executed from 5 different locations. 
     This means that the test is executed from each location every 5 minutes. Note that the tests will not run exactly every minute.
   - The retry has been disabled for demo purposes.
   - The SSL check has been disabled.

1. Locate the `apimSslCertificateCheckAvailabilityTest` resource that deploys the `Standard Test - API Management SSL Certificate Check` test.  

   The purpose of this test is to check that the SSL certificate of API Management is valid and not close to expiring. 
   This test use the standard status endpoint of the API Management service to check for availability. 
   In this case we use `/internal-status-0123456789abcdef` because we've deployed the Consumption tier of API Management. 
   For other API Management iters you can use `/status-0123456789abcdef`.  

   Some things to note about this test:  
   - The SSL check has been enabled and the test will fail if the certificate expires within the configured number of days (default is 30) or has already expired.
   - The frequency is set to 300 seconds (5 minutes) and the test is executed from 5 different locations. 
     This means that the test is executed from each location every 5 minutes. Note that the tests will not run exactly every minute.
     > In a real world scenario, I would set the frequency to the maximum of 900 (15 minutes) and execute the test from a single location
     > because we don't have to be notified the instant that the certificate expires within the configured number of days and it minimizes costs.
     > However, for demo purposes I set the frequency to 300 (5 minutes) and configured multiple locations.
     > This also makes the 'Verify Availability Tests' step in the GitHub Actions workflow succeed faster.
   - The retry has been disabled for demo purposes.

1. Navigate to the Application Insights resource in the Azure portal.
1. Click the `Availability` tab in the left menu.
1. Click on the Edit button (pencil) of the `Standard Test - Backend API Status` test.
1. Note that you can add custom headers to be passed with the request under `Standard test info`. 

   **Do not use secrets here!** The values are stored in plain text in the test definition and Key Vault references are not supported.


### Azure Functions

Here's a class diagram of the generic components used for the availability tests in the Azure Functions:

![Azure Functions - Class Diagram](https://raw.githubusercontent.com/ronaldbosma/track-availability-in-app-insights/refs/heads/main/images/diagrams-functions-class-diagram.png)

Show the Azure Functions implementation. 

1. Open [AvailabilityTest.cs](https://github.com/ronaldbosma/track-availability-in-app-insights/blob/main/src/functionApp/TrackAvailabilityInAppInsights.FunctionApp/AvailabilityTests/AvailabilityTest.cs).  

   This generic class implements the logic to check for availability and publish the results to Application Insights. 
   The class requires a test name, function that performs the availability check and a `TelemetryClient` instance.  

   The `ExecuteAsync` method performs the following steps:
   - An `AvailabilityTelemetry` instance is created which holds the availability test result data.
   - A `Stopwatch` is used to measure the duration of the test.
   - An `Activity` is created to enable distributed tracing and correlation of telemetry in App Insights.  
     Within the activity scope:
     - Several IDs on the activity are configured on the availability telemetry to enable correlation.
     - The start time of the test is set.
     - The method is executed that performs the actual availability check.
   - If an exception is thrown, the message and exception details are added to the availability telemetry instance.
   - At then end
     - The duration of the test is set.
     - The availability telemetry is published to Application Insights.

1. Open [HttpGetRequestAvailabilityTest.cs](https://github.com/ronaldbosma/track-availability-in-app-insights/blob/main/src/functionApp/TrackAvailabilityInAppInsights.FunctionApp/AvailabilityTests/HttpGetRequestAvailabilityTest.cs).  
   
   Most availability test usually perform an HTTP GET request to check for availability. 
   The `HttpGetRequestAvailabilityTest` class wraps the `AvailabilityTest` class and provides a default implementation of the `checkAvailabilityAsync` method to perform a GET request on a configured URL.

1. Open [AvailabilityTestFactory.cs](https://github.com/ronaldbosma/track-availability-in-app-insights/blob/main/src/functionApp/TrackAvailabilityInAppInsights.FunctionApp/AvailabilityTests/AvailabilityTestFactory.cs). This class is a simple factory class to create instances of `IAvailabilityTest`.

1. Open [ApimSslCertificateCheckAvailabilityTest.cs](https://github.com/ronaldbosma/track-availability-in-app-insights/blob/main/src/functionApp/TrackAvailabilityInAppInsights.FunctionApp/ApimSslCertificateCheckAvailabilityTest.cs).  
   
   The function is executed every minute.
   It shows how to use the `AvailabilityTest` class directly, and it checks if the SSL certificate of API Management is nearly expired or already expired.
   The implementation for the check is provided in the `CheckSslCertificateAsync` method, which inturn uses [SslCertificateValidator](https://github.com/ronaldbosma/track-availability-in-app-insights/blob/main/src/functionApp/TrackAvailabilityInAppInsights.FunctionApp/AvailabilityTests/SslCertificateValidator.cs) class.  

   See the following sequence diagram for an overview of the flow:  
   ![Azure Functions - Sequence Diagram](https://raw.githubusercontent.com/ronaldbosma/track-availability-in-app-insights/refs/heads/main/images/diagrams-functions-sequence-diagram.png)

1. Open [BackendStatusAvailabilityTest.cs](https://github.com/ronaldbosma/track-availability-in-app-insights/blob/main/src/functionApp/TrackAvailabilityInAppInsights.FunctionApp/BackendStatusAvailabilityTest.cs).  

   The function is executed every minute.
   It uses the `HttpGetRequestAvailabilityTest` class to perform a HTTP GET request on the `/backend/status` endpoint in API Management to check for availability.  

   See the following sequence diagram for an overview of the flow (it's more complex than the previous but it makes creating a test much easier):  
   ![Azure Functions - Sequence Diagram - Get Request](https://raw.githubusercontent.com/ronaldbosma/track-availability-in-app-insights/refs/heads/main/images/diagrams-functions-sequence-diagram-get-request.png)
   
1. The necessary DI registrations are configured in [ServiceCollectionExtensions.cs](https://github.com/ronaldbosma/track-availability-in-app-insights/blob/main/src/functionApp/TrackAvailabilityInAppInsights.FunctionApp/ServiceCollectionExtensions.cs).

   Note the following setup of Application Insights. These are required to setup logging and for the `TelemetryClient` to be configured.

   ```csharp
   services.AddApplicationInsightsTelemetryWorkerService()
           .ConfigureFunctionsApplicationInsights();
   ```

### Logic App Workflow

Show the Logic App workflow implementation. 

1. Navigate to the Logic App resource in the Azure portal.

1. Open the `backend-availability-test` workflow to show an availability test that performs an HTTP get on the backend API. The designer should open with the following workflow:  

   ![Logic App Workflow](https://raw.githubusercontent.com/ronaldbosma/track-availability-in-app-insights/refs/heads/main/images/logic-app-workflow-backend-status.png)

   Some things to note about this workflow:
   - It triggers every minute.
   - A `TestName` variable is set.
   - A timer is started.
   - In the `HTTP` action a call to the `/backend/status` endpoint in API Management is made.
   - Depending on the response:
     - if successful, the backend is tracked as available in Application Insights
     - if failed, the backend is tracked as unavailable in Application Insights

1. The 'Track is (un)available...' actions use custom functions that are deployed inside the Logic App along side the workflow. 
   Open [AvailabilityTestFunctions.cs](https://github.com/ronaldbosma/track-availability-in-app-insights/blob/main/src/logicApp/Functions/AvailabilityTestFunctions.cs) to view the implementation.  

   - A `TelemetryClient` instance is created in the constructor of the class.
   - The `TrackAvailability` and `TrackUnavailability` functions use the `TrackAvailability` method that:
     - Creates an `AvailabilityTelemetry` object with the test results
     - Creates an `Activity` to enable distributed tracing and correlation of telemetry in App Insights. 
     - Publishes the availability telemetry to Application Insights.

1. Open the `apim-ssl-certificate-check-availability-test` workflow to show an availability test that checks the SSL certificate expiry of API Management. The designer should open with the following workflow:  

   ![Logic App Workflow](https://raw.githubusercontent.com/ronaldbosma/track-availability-in-app-insights/refs/heads/main/images/logic-app-workflow-ssl-cert-check.png)

   Some things to note about this workflow:
   - Similarly to the previous workflow, it triggers every minute, sets a `TestName` variable and starts a timer.
   - It uses the `GetSslServerCertificateExpirationInDays` function to get the number of days until the SSL certificate expires. See [SslServerCertificateFunctions.cs](https://github.com/ronaldbosma/track-availability-in-app-insights/blob/main/src/logicApp/Functions/SslServerCertificateFunctions.cs) for the implementation of this function.
   - Depending on the number of days until expiry:
     - if less than or equal to the configured number of days, the test is tracked as unavailable in Application Insights (meaning that the certificate is nearly expired or already expired) and the workflow status is set to failed
     - if greater than the configured number of days (default is 30), the test is tracked as available in Application Insights (meaning that the certificate is valid)
   - If determining the expiry fails, the test is also tracked as unavailable in Application Insights.

### SSL Certificate Expiry

Show the SSL certificate expiry check.

1. Change the SSL certificate expiry threshold as described [here](https://github.com/ronaldbosma/track-availability-in-app-insights/tree/main#ssl-certificate-remaining-lifetime-days) to 365 days.

1. Redeploy the infrastructure using `azd provision`.

1. Navigate to the `Availability` tab in the Application Insights resource in the Azure portal.

1. Wait for the various 'SSL Certificate Check' availability tests to execute and fail because the certificate is expiring within 365 days.


### Alerts

The backend API has been implemented to return a `503 Service Unavailable` status code for an approximate percentage of the time. 
See the [Approximate failure percentage](https://github.com/ronaldbosma/track-availability-in-app-insights/tree/main#approximate-failure-percentage) section in the README for more information.

There are two alerts included in the templates:
- Failed availability test, which triggers when any of the availability tests fail.
- Failed requests, which triggers when a failed request is logged in App Insights.

Follow these steps to view the alerts:

1. In the Azure Portal, navigate to Azure Monitor.

1. Click on `Alerts` in the left menu.

1. If an availability test or request failed, you should see alerts fired:  

   ![Alerts](https://raw.githubusercontent.com/ronaldbosma/track-availability-in-app-insights/refs/heads/main/images/alerts.png)
   
1. For a 'failed availability test' alert, to see which test failed:

   1. Click on the alert to view the details. 

   1. Expand the 'Additional details'. The `availabilityResult/name` dimension specifies which test has failed.  

      ![Alert Details](https://raw.githubusercontent.com/ronaldbosma/track-availability-in-app-insights/refs/heads/main/images/alert-details.png)

1. For a 'failed requests' alert, to see which requests failed:

   1. Click on the alert to view the details. 

   1. Click on the 'View results in Logs' link to view the failed requests in App Insights.

1. Open the [alerts.bicep](https://github.com/ronaldbosma/track-availability-in-app-insights/blob/main/infra/modules/application/alerts.bicep) file to review the Bicep definition.  

   1. An alert is created that is triggered when any of the availability tests fail. 
      This way we can also monitor availability tests that are exectuted from an Azure Function or Logic App workflow.  
      
      - The alert is evaluated every 5 minutes.
      - The alert triggers when an availability test doesn't succeed 100% of the time in the last 5 minutes.
      - By using a dimension, the alert is triggered for each test separately.

   1. An alert is created that is triggered when failed requests are logged in App Insights.
      - The alert is evaluated every 5 minutes.
      - The alert triggers when there is at least 1 failed request in the last 5 minutes.

#### Email Notification

The alerts have an action group configured that can send notifications and take actions when an alert is triggered.
By default, no notification and actions are configured, but you can easily configure email notifications by following these steps:

1. Configure an email address to send alerts to as described [here](https://github.com/ronaldbosma/track-availability-in-app-insights/tree/main#alert-recipient-email-address).

1. Redeploy the infrastructure using `azd provision`.

1. When an availability test or request fails, an email notification is sent to the configured email address.

1. The subject of the email will contain the name of the alert that was fired/resolved. 
   - For a 'failed availability test' alert, open the email and locate the `Dimensions.Dimension value1` property to see to which test the alert corresponds.
   - For a 'failed requests' alert, open the email and click on the 'View query results' link to view the failed requests in App Insights.
