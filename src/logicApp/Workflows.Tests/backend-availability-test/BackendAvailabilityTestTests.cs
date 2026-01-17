using Microsoft.Azure.Workflows.UnitTesting.Definitions;
using Newtonsoft.Json.Linq;
using System.Net;
using TrackAvailabilityInAppInsights.LogicApp.Workflows.Tests.MockOutputs;

namespace TrackAvailabilityInAppInsights.LogicApp.Workflows.Tests
{
    [TestClass]
    public class BackendAvailabilityTestTests
    {
        private readonly TestExecutor _testExecutor = new("backend-availability-test");

        private static class ActionNames
        {
            public const string Http = "HTTP";
            public const string StartTime = "Start_time";
            public const string TrackIsAvailable = "Track_is_available_(in_App_Insights)";
            public const string TrackIsUnavailable = "Track_is_unavailable_(in_App_Insights)";
        }

        [TestMethod]
        public async Task RunWorkflow_BackendIsAvailable_AvailabilitySuccessTrackedAndWorkflowSucceeds()
        {
            // Arrange
            var httpSuccessResponse = new HTTPActionOutput(HttpStatusCode.OK);
            var httpActionMock = new HTTPActionMock(name: ActionNames.Http, outputs: httpSuccessResponse);

            var trackIsAvailableMock = new InvokeFunctionActionMock(name: ActionNames.TrackIsAvailable);

            // Act
            var testRun = await _testExecutor.RunWorkflowAsync(new RecurrenceTriggerMock(), [httpActionMock, trackIsAvailableMock]);

            // Assert
            Assert.AreEqual(TestWorkflowStatus.Succeeded, testRun.Status);

            var expectedParameters = new JObject
            {
                { "testName", "Logic App Workflow - Backend API Status" },
                { "startTime", testRun.GetAction(ActionNames.StartTime).Outputs["body"]?.ToString() }
            };
            testRun.VerifyFunctionWasInvoked(ActionNames.TrackIsAvailable, FunctionNames.TrackIsAvailable, expectedParameters);

            testRun.VerifyActionWasSkipped(ActionNames.TrackIsUnavailable);
        }

        [TestMethod]
        public async Task RunWorkflow_BackendIsUnavailable_AvailabilityFailureTrackedAndWorkflowFails()
        {
            // Arrange
            var httpServiceUnavailableResponse = new HTTPActionOutput(HttpStatusCode.ServiceUnavailable);
            var httpActionMock = new HTTPActionMock(TestWorkflowStatus.Failed, name: ActionNames.Http, outputs: httpServiceUnavailableResponse);

            var trackIsUnavailableMock = new InvokeFunctionActionMock(name: ActionNames.TrackIsUnavailable);

            // Act
            var testRun = await _testExecutor.RunWorkflowAsync(new RecurrenceTriggerMock(), [httpActionMock, trackIsUnavailableMock]);

            // Assert
            Assert.AreEqual(TestWorkflowStatus.Failed, testRun.Status);

            var expectedParameters = new JObject
            {
                { "testName", "Logic App Workflow - Backend API Status" },
                { "startTime", testRun.GetAction(ActionNames.StartTime).Outputs["body"]?.ToString() },
                { "message", "HTTP call failed with status code 503 and response body: \"\"" }
            };
            testRun.VerifyFunctionWasInvoked(ActionNames.TrackIsUnavailable, FunctionNames.TrackIsUnavailable, expectedParameters);

            testRun.VerifyActionWasSkipped(ActionNames.TrackIsAvailable);
        }
    }
}
