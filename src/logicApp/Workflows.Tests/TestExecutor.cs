using Microsoft.Azure.Workflows.UnitTesting;
using Microsoft.Azure.Workflows.UnitTesting.Definitions;
using Microsoft.Extensions.Configuration;

namespace TrackAvailabilityInAppInsights.LogicApp.Workflows.Tests
{
    public class TestExecutor
    {
        private readonly string _rootDirectory = "../../../../";
        private readonly string _logicAppName = "Workflows";
        private readonly string _workflow;

        public TestExecutor(string workflow)
        {
            _workflow = workflow;
        }

        public async Task<TestWorkflowRun> RunWorkflowAsync(TriggerMock triggerMock, ActionMock[] actionMocks)
        {
            var testMock = new TestMockDefinition(
                triggerMock: triggerMock,   
                actionMocks: new Dictionary<string, ActionMock>(
                    actionMocks.Select(a => new KeyValuePair<string, ActionMock>(a.Name, a))
                )
            );

            var testRun = await Create().RunWorkflowAsync(testMock: testMock).ConfigureAwait(continueOnCapturedContext: false);
            Assert.IsNotNull(testRun, "No test workflow run returned");

            return testRun!;
        }

        public UnitTestExecutor Create()
        {
            // Set the path for workflow-related input files in the workspace and build the full paths to the required JSON files.
            var workflowDefinitionPath = Path.Combine(_rootDirectory, _logicAppName, _workflow, "workflow.json");
            var connectionsPath = Path.Combine(_rootDirectory, _logicAppName, "connections.json");
            var parametersPath = Path.Combine(_rootDirectory, _logicAppName, "parameters.json");
            
            return new UnitTestExecutor(
                workflowFilePath: workflowDefinitionPath,
                connectionsFilePath: connectionsPath,
                parametersFilePath: parametersPath,
                localSettingsFilePath: "workflows.settings.json"
            );
        }
    }
}