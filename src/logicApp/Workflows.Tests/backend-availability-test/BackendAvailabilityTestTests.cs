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

        [TestMethod]
        public async Task RunWorkflow_BackendIsAvailable_AvailabilitySuccessTrackedAndWorkflowSucceeds()
        {
            // Arrange
            var trigger = new RecurrenceTriggerMock();

            var httpSuccessResponse = new HTTPActionOutput() { StatusCode = HttpStatusCode.OK };
            var httpActionMock = new HTTPActionMock(name: "HTTP", outputs: httpSuccessResponse);

            var trackIsAvailableMock = new InvokeFunctionActionMock<JObject>(name: "Track_is_available_(in_App_Insights)", outputBody: []);

            // Act
            var testMock = new TestMockDefinition(
                triggerMock: trigger,
                actionMocks: new Dictionary<string, ActionMock>()
                {
                    {httpActionMock.Name, httpActionMock},
                    {trackIsAvailableMock.Name, trackIsAvailableMock }
                }
            );
            var testRun = await this._testExecutor
                .Create()
                .RunWorkflowAsync(testMock: testMock).ConfigureAwait(continueOnCapturedContext: false);

            // Assert
            Assert.IsNotNull(value: testRun);
            Assert.AreEqual(expected: TestWorkflowStatus.Succeeded, actual: testRun.Status);
        }
    }
}
