using Microsoft.Azure.Workflows.UnitTesting.Definitions;
using Newtonsoft.Json.Linq;
using System.Net;
using TrackAvailabilityInAppInsights.LogicApp.Workflows.Tests.MockOutputs;
using Workflows.Tests.Mocks.backend_availability_test;

namespace TrackAvailabilityInAppInsights.LogicApp.Workflows.Tests
{
    [TestClass]
    public class BackendAvailabilityTestTests
    {
        private readonly TestExecutor _testExecutor = new("backend-availability-test/testSettings.config");

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
            var trigger = new RecurrenceTriggerMock();

            var httpSuccessResponse = new HTTPActionOutput() { StatusCode = HttpStatusCode.OK };
            var httpActionMock = new HTTPActionMock(name: ActionNames.Http, outputs: httpSuccessResponse);

            var trackIsAvailableOutput = new InvokeFunctionActionOutput<JObject> { Body = [] };
            var trackIsAvailableMock = new InvokeFunctionActionMock<JObject>(name: ActionNames.TrackIsAvailable, outputs: trackIsAvailableOutput);

            // Act
            var testRun = await _testExecutor.RunWorkflowAsync(trigger, [httpActionMock, trackIsAvailableMock]);

            // Assert
            Assert.IsNotNull(testRun);
            Assert.AreEqual(TestWorkflowStatus.Succeeded, testRun.Status);

            var expectedParameters = new JObject
            {
                { "testName", "Logic App Workflow - Backend API Status" },
                { "startTime", testRun.Actions[ActionNames.StartTime].Outputs["body"].ToString() }
            };
            testRun.VerifyFunctionWasInvoked(ActionNames.TrackIsAvailable, FunctionNames.TrackIsAvailable, expectedParameters);

            testRun.VerifyActionStatus(ActionNames.TrackIsUnavailable, TestWorkflowStatus.Skipped);
        }

        [TestMethod]
        public async Task RunWorkflow_BackendIsUnavailable_AvailabilityFailureTrackedAndWorkflowFails()
        {
            // Arrange
            var trigger = new RecurrenceTriggerMock();

            var httpServiceUnavailableResponse = new HTTPActionOutput() { StatusCode = HttpStatusCode.ServiceUnavailable };
            var httpActionMock = new HTTPActionMock(TestWorkflowStatus.Failed, name: ActionNames.Http, outputs: httpServiceUnavailableResponse);

            var trackIsUnavailableOutput = new InvokeFunctionActionOutput<JObject> { Body = [] };
            var trackIsUnavailableMock = new InvokeFunctionActionMock<JObject>(name: ActionNames.TrackIsUnavailable, outputs: trackIsUnavailableOutput);

            // Act
            var testRun = await _testExecutor.RunWorkflowAsync(trigger, [httpActionMock, trackIsUnavailableMock]);

            // Assert
            Assert.IsNotNull(testRun);
            Assert.AreEqual(TestWorkflowStatus.Failed, testRun.Status);

            var expectedParameters = new JObject
            {
                { "testName", "Logic App Workflow - Backend API Status" },
                { "startTime", testRun.Actions[ActionNames.StartTime].Outputs["body"].ToString() },
                { "message", "HTTP call failed with status code 503 and response body: \"{}\"" }
            };
            testRun.VerifyFunctionWasInvoked(ActionNames.TrackIsUnavailable, FunctionNames.TrackIsUnavailable, expectedParameters);

            testRun.VerifyActionStatus(ActionNames.TrackIsAvailable, TestWorkflowStatus.Skipped);
        }
    }
}
