using Microsoft.Azure.Workflows.UnitTesting;
using Microsoft.Azure.Workflows.UnitTesting.Definitions;
using Microsoft.Extensions.Configuration;

namespace TrackAvailabilityInAppInsights.LogicApp.Workflows.Tests
{
    public class TestExecutor
    {
        private readonly string _rootDirectory;
        private readonly string _logicAppName;
        private readonly string _workflow;

        public TestExecutor(string configPath)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddXmlFile(configPath, optional: false, reloadOnChange: true)
                .Build();

            _rootDirectory = configuration["TestSettings:WorkspacePath"];
            _logicAppName = configuration["TestSettings:LogicAppName"];
            _workflow = configuration["TestSettings:WorkflowName"];
        }

        public async Task<TestWorkflowRun> RunWorkflowAsync(TriggerMock triggerMock, ActionMock[] actionMocks)
        {
            var testMock = new TestMockDefinition(
                triggerMock: triggerMock,
                actionMocks: new Dictionary<string, ActionMock>(
                    actionMocks.Select(a => new KeyValuePair<string, ActionMock>(a.Name, a))
                )
            );

            return await Create().RunWorkflowAsync(testMock: testMock).ConfigureAwait(continueOnCapturedContext: false);
        }

        public UnitTestExecutor Create()
        {
            // Set the path for workflow-related input files in the workspace and build the full paths to the required JSON files.
            var workflowDefinitionPath = Path.Combine(this._rootDirectory, this._logicAppName, this._workflow, "workflow.json");
            var connectionsPath = Path.Combine(this._rootDirectory, this._logicAppName, "connections.json");
            var parametersPath = Path.Combine(this._rootDirectory, this._logicAppName, "parameters.json");
            var localSettingsPath = Path.Combine(this._rootDirectory, this._logicAppName, "cloud.settings.json");
            
            return new UnitTestExecutor(
                workflowFilePath: workflowDefinitionPath,
                connectionsFilePath: connectionsPath,
                parametersFilePath: parametersPath,
                localSettingsFilePath: localSettingsPath
            );
        }
    }
}