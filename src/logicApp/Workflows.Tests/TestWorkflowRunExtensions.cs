using Microsoft.Azure.Workflows.UnitTesting.Definitions;
using Newtonsoft.Json.Linq;

namespace TrackAvailabilityInAppInsights.LogicApp.Workflows.Tests
{
    internal static class TestWorkflowRunExtensions
    {
        public static void VerifyFunctionWasInvoked(this TestWorkflowRun testRun, string actionName, string expectedFunctionName, JObject expectedParameters, TestWorkflowStatus expectedStatus = TestWorkflowStatus.Succeeded)
        {
            var action = testRun.GetAction(actionName);
            Assert.AreEqual(expectedFunctionName, action.Inputs["functionName"]);

            var actualParameters = (JObject)action.Inputs["parameters"];
            Assert.AreEqual(expectedParameters.Count, actualParameters.Count);
            foreach (var expectedParameter in expectedParameters)
            {
                Assert.IsTrue(actualParameters.ContainsKey(expectedParameter.Key), $"Parameter with name {expectedParameter.Key} not found");
                Assert.AreEqual(expectedParameter.Value, actualParameters[expectedParameter.Key], $"Unexpected value for parameter {expectedParameter.Key}");
            }

            Assert.AreEqual(expectedStatus, action.Status, $"Unexpected status for action: {actionName}");
        }

        public static void VerifyActionStatus(this TestWorkflowRun testRun, string actionName, TestWorkflowStatus expectedStatus)
        {
            var action = testRun.GetAction(actionName);
            Assert.AreEqual(expectedStatus, action.Status, $"Unexpected status for action: {actionName}");
        }

        public static TestWorkflowRunActionResult GetAction(this TestWorkflowRun testRun, string actionName)
        {
            Assert.IsTrue(testRun.Actions.ContainsKey(actionName), $"Action with name {actionName} not found");
            return testRun.Actions[actionName];
        }
    }
}
