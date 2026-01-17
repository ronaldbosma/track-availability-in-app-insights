using Microsoft.Azure.Workflows.UnitTesting.Definitions;
using Newtonsoft.Json.Linq;
using TrackAvailabilityInAppInsights.LogicApp.Workflows.Tests.MockOutputs;
using Workflows.Tests.Mocks.backend_availability_test;

namespace TrackAvailabilityInAppInsights.LogicApp.Workflows.Tests
{
    [TestClass]
    public class ApimSslCertificateCheckAvailabilityTestTests
    {
        private readonly TestExecutor _testExecutor = new("apim-ssl-certificate-check-availability-test/testSettings.config");

        private static class ActionNames
        {
            public const string GetApimSslServerCertificateExpirationInDays = "Get_APIM_SSL_server_certificate_expiration_in_days";
            public const string StartTime = "Start_time";
            public const string TrackCertificateExpiration = "Track_certificate_expiration_(in_App_Insights)";
            public const string TrackIsAvailable = "Track_is_available_(in_App_Insights)";
            public const string TrackIsUnavailable = "Track_is_unavailable_(in_App_Insights)";
        }

        [TestMethod]
        public async Task RunWorkflow_CertificateIsValid_AvailabilitySuccessTrackedAndWorkflowSucceeds()
        {
            // Arrange
            var trigger = new RecurrenceTriggerMock();

            var getCertificateExpirationInDaysOutput = new InvokeFunctionActionOutput<int> { Body = 365 };
            var getCertificateExpirationInDaysMock = new InvokeFunctionActionMock<int>(name: ActionNames.GetApimSslServerCertificateExpirationInDays, outputs: getCertificateExpirationInDaysOutput);

            var trackIsAvailableOutput = new InvokeFunctionActionOutput<JObject> { Body = [] };
            var trackIsAvailableMock = new InvokeFunctionActionMock<JObject>(name: ActionNames.TrackIsAvailable, outputs: trackIsAvailableOutput);

            // Act
            var testRun = await _testExecutor.RunWorkflowAsync(trigger, [getCertificateExpirationInDaysMock, trackIsAvailableMock]);

            // Assert
            Assert.IsNotNull(testRun);
            Assert.AreEqual(TestWorkflowStatus.Succeeded, testRun.Status);

            var expectedParameters = new JObject
            {
                { "testName", "Logic App Workflow - API Management SSL Certificate Check" },
                { "startTime", testRun.Actions[ActionNames.StartTime].Outputs["body"].ToString() }
            };
            testRun.VerifyFunctionWasInvoked(ActionNames.TrackIsAvailable, FunctionNames.TrackIsAvailable, expectedParameters, TestWorkflowStatus.Succeeded);

            testRun.VerifyActionStatus(ActionNames.TrackCertificateExpiration, TestWorkflowStatus.Skipped);
            testRun.VerifyActionStatus(ActionNames.TrackIsUnavailable, TestWorkflowStatus.Skipped);
        }
    }
}
