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
1. Click the `Availability` tab in the left menu.

   The result should look like this:

   ![Availability Test Results](https://raw.githubusercontent.com/ronaldbosma/track-availability-in-app-insights/refs/heads/main/images/availability-test-results.png)

#### Standard Test Result

Show details of a standard test.

1. Select the `Standard Test - Backend API Status` test.
1. Click on either `Successful` or `Failed`.
1. Click on an availability test result in the right pane to view the details.
1. Show the information in the end-to-end transaction details screen. Note that the API Mangement request logging is included.  

   ![Standard Test - End-to-end Transaction Details](https://raw.githubusercontent.com/ronaldbosma/track-availability-in-app-insights/refs/heads/main/images/standard-test-end-to-end-transaction-details.png)

1. Close the end-to-end transaction details.

#### Azure Function Test Result

Show details of a test executed from an Azure Function.

1. Select the `Azure Function - Backend API Status` test.
1. Click on either `Successful` or `Failed`.
1. Click on an availability test result in the right pane to view the details.
1. Show the information in the end-to-end transaction details screen. Both the logging of the Azure Function and the API Management request logging are included.  

   ![Azure Function - End-to-end Transaction Details](https://raw.githubusercontent.com/ronaldbosma/track-availability-in-app-insights/refs/heads/main/images/azure-function-end-to-end-transaction-details.png)

1. Close the end-to-end transaction details.

#### Logic App Workflow Test Result

Show details of a test executed from an Logic App Workflow.

1. Select the `Logic App Workflow - Backend API Status` test.
1. Click on either `Successful` or `Failed`.
1. Click on an availability test result in the right pane to view the details.
1. Show the information in the end-to-end transaction details screen. Both the logging of the Logic App Workflow and the API Management request logging are included. The timeline is a little bit 'messy' compared to the others because of the way the Logic App tracks the availability result as a separate action.  

   ![Logic App Workflow - End-to-end Transaction Details](https://raw.githubusercontent.com/ronaldbosma/track-availability-in-app-insights/refs/heads/main/images/logic-app-workflow-end-to-end-transaction-details.png)

1. Close the end-to-end transaction details.