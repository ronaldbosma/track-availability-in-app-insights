using Microsoft.Azure.Workflows.UnitTesting.Definitions;
using Microsoft.Azure.Workflows.UnitTesting.ErrorResponses;

namespace TrackAvailabilityInAppInsights.LogicApp.Workflows.Tests.MockOutputs
{
    /// <summary>
    /// The <see cref="RecurrenceTriggerMock"/> class.
    /// </summary>
    public class RecurrenceTriggerMock : TriggerMock
    {
        /// <summary>
        /// Creates a mocked instance for  <see cref="RecurrenceTriggerMock"/> with static outputs.
        /// </summary>
        public RecurrenceTriggerMock(TestWorkflowStatus status = TestWorkflowStatus.Succeeded, string? name = null, RecurrenceTriggerOutput? outputs = null)
            : base(status: status, name: name, outputs: outputs ?? new RecurrenceTriggerOutput())
        {
        }

        /// <summary>
        /// Creates a mocked instance for  <see cref="RecurrenceTriggerMock"/> with static error info.
        /// </summary>
        public RecurrenceTriggerMock(TestWorkflowStatus status, string? name = null, TestErrorInfo? error = null)
            : base(status: status, name: name, error: error)
        {
        }

        /// <summary>
        /// Creates a mocked instance for <see cref="RecurrenceTriggerMock"/> with a callback function for dynamic outputs.
        /// </summary>
        public RecurrenceTriggerMock(Func<TestExecutionContext, RecurrenceTriggerMock> onGetTriggerMock, string? name = null)
            : base(onGetTriggerMock: onGetTriggerMock, name: name)
        {
        }
    }

    /// <summary>
    /// Class for RecurrenceTriggerOutput representing an empty object.
    /// </summary>
    public class RecurrenceTriggerOutput : MockOutput
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RecurrenceTriggerOutput"/> class.
        /// </summary>
        public RecurrenceTriggerOutput()
        {
        }
    }
}