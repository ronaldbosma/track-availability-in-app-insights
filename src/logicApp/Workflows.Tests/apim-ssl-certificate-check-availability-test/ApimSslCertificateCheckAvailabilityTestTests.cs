using Microsoft.Azure.Workflows.Common.ErrorResponses;
using Microsoft.Azure.Workflows.UnitTesting.Definitions;
using Microsoft.Azure.Workflows.UnitTesting.ErrorResponses;
using Newtonsoft.Json.Linq;
using TrackAvailabilityInAppInsights.LogicApp.Workflows.Tests.MockOutputs;

namespace TrackAvailabilityInAppInsights.LogicApp.Workflows.Tests
{
    [TestClass]
    public class ApimSslCertificateCheckAvailabilityTestTests
    {
        private readonly TestExecutor _testExecutor = new("apim-ssl-certificate-check-availability-test");

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
            var expirationInDays = 31;

            var getCertificateExpirationInDaysOutput = new InvokeFunctionActionOutput<int>(expirationInDays);
            var getCertificateExpirationInDaysMock = new InvokeFunctionActionMock<int>(name: ActionNames.GetApimSslServerCertificateExpirationInDays, outputs: getCertificateExpirationInDaysOutput);

            var trackIsAvailableMock = new InvokeFunctionActionMock(name: ActionNames.TrackIsAvailable);

            // Act
            var testRun = await _testExecutor.RunWorkflowAsync(new RecurrenceTriggerMock(), [getCertificateExpirationInDaysMock, trackIsAvailableMock]);

            // Assert
            Assert.AreEqual(TestWorkflowStatus.Succeeded, testRun.Status);

            var expectedParameters = new JObject
            {
                { "testName", "Logic App Workflow - API Management SSL Certificate Check" },
                { "startTime", testRun.GetAction(ActionNames.StartTime).Outputs["body"]?.ToString() }
            };
            testRun.VerifyFunctionWasInvoked(ActionNames.TrackIsAvailable, FunctionNames.TrackIsAvailable, expectedParameters);

            testRun.VerifyActionWasSkipped(ActionNames.TrackCertificateExpiration);
            testRun.VerifyActionWasSkipped(ActionNames.TrackIsUnavailable);
        }

        [TestMethod]
        public async Task RunWorkflow_CertificateExpiresIn30Days_AvailabilityFailureTrackedAndWorkflowFails()
        {
            // Arrange
            var expirationInDays = 30;

            var getCertificateExpirationInDaysOutput = new InvokeFunctionActionOutput<int>(expirationInDays);
            var getCertificateExpirationInDaysMock = new InvokeFunctionActionMock<int>(name: ActionNames.GetApimSslServerCertificateExpirationInDays, outputs: getCertificateExpirationInDaysOutput);

            var trackCertificateExpirationMock = new InvokeFunctionActionMock(name: ActionNames.TrackCertificateExpiration);

            // Act
            var testRun = await _testExecutor.RunWorkflowAsync(new RecurrenceTriggerMock(), [getCertificateExpirationInDaysMock, trackCertificateExpirationMock]);

            // Assert
            Assert.AreEqual(TestWorkflowStatus.Failed, testRun.Status);

            var expectedParameters = new JObject
            {
                { "testName", "Logic App Workflow - API Management SSL Certificate Check" },
                { "startTime", testRun.GetAction(ActionNames.StartTime).Outputs["body"]?.ToString() },
                { "message", $"SSL server certificate for sample.azure-api.net is expiring in {expirationInDays} days" }
            };
            testRun.VerifyFunctionWasInvoked(ActionNames.TrackCertificateExpiration, FunctionNames.TrackIsUnavailable, expectedParameters);

            testRun.VerifyActionWasSkipped(ActionNames.TrackIsAvailable);
            testRun.VerifyActionWasSkipped(ActionNames.TrackIsUnavailable);
        }

        [TestMethod]
        public async Task RunWorkflow_CertificateHasExpired_AvailabilityFailureTrackedAndWorkflowFails()
        {
            // Arrange
            var expirationInDays = -5;

            var getCertificateExpirationInDaysOutput = new InvokeFunctionActionOutput<int>(expirationInDays);
            var getCertificateExpirationInDaysMock = new InvokeFunctionActionMock<int>(name: ActionNames.GetApimSslServerCertificateExpirationInDays, outputs: getCertificateExpirationInDaysOutput);

            var trackCertificateExpirationMock = new InvokeFunctionActionMock(name: ActionNames.TrackCertificateExpiration);

            // Act
            var testRun = await _testExecutor.RunWorkflowAsync(new RecurrenceTriggerMock(), [getCertificateExpirationInDaysMock, trackCertificateExpirationMock]);

            // Assert
            Assert.AreEqual(TestWorkflowStatus.Failed, testRun.Status);

            var expectedParameters = new JObject
            {
                { "testName", "Logic App Workflow - API Management SSL Certificate Check" },
                { "startTime", testRun.GetAction(ActionNames.StartTime).Outputs["body"]?.ToString() },
                { "message", $"SSL server certificate for sample.azure-api.net is expiring in {expirationInDays} days" }
            };
            testRun.VerifyFunctionWasInvoked(ActionNames.TrackCertificateExpiration, FunctionNames.TrackIsUnavailable, expectedParameters);

            testRun.VerifyActionWasSkipped(ActionNames.TrackIsAvailable);
            testRun.VerifyActionWasSkipped(ActionNames.TrackIsUnavailable);
        }

        [TestMethod]
        public async Task RunWorkflow_DeterminationOfCertificateExpirationFailed_AvailabilityFailureTrackedAndWorkflowFails()
        {
            // Arrange
            var error = new TestErrorInfo(ErrorResponseCode.InvokeFunctionFailed, "The function 'GetSslServerCertificateExpirationInDays' failed to execute. Please verify function code is valid.");
            var getCertificateExpirationInDaysMock = new InvokeFunctionActionMock<int>(TestWorkflowStatus.Failed, name: ActionNames.GetApimSslServerCertificateExpirationInDays, error);

            var trackIsUnavailableMock = new InvokeFunctionActionMock(name: ActionNames.TrackIsUnavailable);

            // Act
            var testRun = await _testExecutor.RunWorkflowAsync(new RecurrenceTriggerMock(), [getCertificateExpirationInDaysMock, trackIsUnavailableMock]);

            // Assert
            Assert.AreEqual(TestWorkflowStatus.Failed, testRun.Status);

            var expectedParameters = new JObject
            {
                { "testName", "Logic App Workflow - API Management SSL Certificate Check" },
                { "startTime", testRun.GetAction(ActionNames.StartTime).Outputs["body"]?.ToString() },
                { "message", "Unable to determine APIM SSL server certificate expiration" }
            };
            testRun.VerifyFunctionWasInvoked(ActionNames.TrackIsUnavailable, FunctionNames.TrackIsUnavailable, expectedParameters);

            testRun.VerifyActionWasSkipped(ActionNames.TrackIsAvailable);
            testRun.VerifyActionWasSkipped(ActionNames.TrackCertificateExpiration);
        }
    }
}
