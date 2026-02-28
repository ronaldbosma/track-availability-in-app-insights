using Microsoft.Azure.Workflows.UnitTesting.Definitions;

using Newtonsoft.Json.Linq;

namespace TrackAvailabilityInAppInsights.LogicApp.Workflows.Tests
{
    /// <summary>
    /// Extension methods for <seealso cref="TestWorkflowRun"/> to help with test verification.
    /// </summary>
    internal static class TestWorkflowRunExtensions
    {
        public static void VerifyFunctionWasInvoked(this TestWorkflowRun testRun, string actionName, string expectedFunctionName, JObject expectedParameters, TestWorkflowStatus expectedStatus = TestWorkflowStatus.Succeeded)
        {
            var action = testRun.GetAction(actionName);
            Assert.AreEqual(expectedFunctionName, action.Inputs["functionName"]);

            var actualParameters = action.Inputs["parameters"] as JObject;
            Assert.IsNotNull(actualParameters, "Input with name 'parameters' not found or not of type JObect");
            Assert.AreEqual(expectedParameters.Count, actualParameters.Count);
            foreach (var expectedParameter in expectedParameters)
            {
                Assert.IsTrue(actualParameters.ContainsKey(expectedParameter.Key), $"Parameter with name {expectedParameter.Key} not found");
                Assert.AreEqual(expectedParameter.Value, actualParameters[expectedParameter.Key], $"Unexpected value for parameter {expectedParameter.Key}");
            }

            Assert.AreEqual(expectedStatus, action.Status, $"Unexpected status for action: {actionName}");
        }

        public static void VerifyActionWasSkipped(this TestWorkflowRun testRun, string actionName)
        {
            testRun.VerifyActionStatus(actionName, TestWorkflowStatus.Skipped);
        }

        public static void VerifyActionStatus(this TestWorkflowRun testRun, string actionName, TestWorkflowStatus expectedStatus)
        {
            var action = testRun.GetAction(actionName);
            Assert.AreEqual(expectedStatus, action.Status, $"Unexpected status for action: {actionName}");
        }

        public static TestWorkflowRunActionResult GetAction(this TestWorkflowRun testRun, string actionName)
        {
            var action = testRun.Actions.FindActionRecursively(actionName);
            Assert.IsNotNull(action, $"Action with name {actionName} not found");
            return action!;
        }

        private static TestWorkflowRunActionResult? FindActionRecursively(this IDictionary<string, TestWorkflowRunActionResult>? actions, string actionName)
        {
            if (actions == null)
            {
                return null;
            }

            if (actions.TryGetValue(actionName, out TestWorkflowRunActionResult? value))
            {
                return value;
            }

            foreach (var action in actions.Values)
            {
                if (action.ChildActions != null)
                {
                    var foundAction = action.ChildActions.FindActionRecursively(actionName);
                    if (foundAction != null)
                    {
                        return foundAction;
                    }
                }
            }

            return null;
        }
    }
}