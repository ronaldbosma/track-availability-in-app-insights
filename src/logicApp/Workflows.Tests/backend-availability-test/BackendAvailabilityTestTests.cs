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
            public const string TrackIsAvailable = "Track_is_available_(in_App_Insights)";
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
            Assert.IsNotNull(value: testRun);
            Assert.AreEqual(expected: TestWorkflowStatus.Succeeded, actual: testRun.Status);
        }
    }
}
