using Microsoft.Azure.Workflows.UnitTesting.Definitions;
using Microsoft.Azure.Workflows.UnitTesting.ErrorResponses;
using Newtonsoft.Json.Linq;

namespace TrackAvailabilityInAppInsights.LogicApp.Workflows.Tests.MockOutputs
{
    /// <summary>
    /// The <see cref="InvokeFunctionActionMock"/> class.
    /// </summary>
    public class InvokeFunctionActionMock : InvokeFunctionActionMock<JObject>
    {
        /// <summary>
        /// Creates a mocked instance for  <see cref="InvokeFunctionActionMock"/> with static outputs.
        /// </summary>
        public InvokeFunctionActionMock(TestWorkflowStatus status = TestWorkflowStatus.Succeeded, string? name = null, InvokeFunctionActionOutput<JObject>? outputs = null)
            : base(status: status, name: name, outputs: outputs ?? new InvokeFunctionActionOutput<JObject>([]))
        {
        }
    }

    /// <summary>
    /// The <see cref="InvokeFunctionActionMock<typeparamref name="TBody"/>"/> class.
    /// </summary>
    public class InvokeFunctionActionMock<TBody> : ActionMock
    {
        /// <summary>
        /// Creates a mocked instance for  <see cref="InvokeFunctionActionMock<typeparamref name="TBody"/>"/> with static outputs.
        /// </summary>
        public InvokeFunctionActionMock(TestWorkflowStatus status = TestWorkflowStatus.Succeeded, string? name = null, InvokeFunctionActionOutput<TBody>? outputs = null)
            : base(status: status, name: name, outputs: outputs ?? new InvokeFunctionActionOutput<TBody>())
        {
        }

        /// <summary>
        /// Creates a mocked instance for  <see cref="InvokeFunctionActionMock<typeparamref name="TBody"/>"/> with static error info.
        /// </summary>
        public InvokeFunctionActionMock(TestWorkflowStatus status, string? name = null, TestErrorInfo? error = null)
            : base(status: status, name: name, error: error)
        {
        }

        /// <summary>
        /// Creates a mocked instance for <see cref="InvokeFunctionActionMock<typeparamref name="TBody"/>"/> with a callback function for dynamic outputs.
        /// </summary>
        public InvokeFunctionActionMock(Func<TestExecutionContext, InvokeFunctionActionMock<TBody>> onGetActionMock, string? name = null)
            : base(onGetActionMock: onGetActionMock, name: name)
        {
        }
    }

    /// <summary>
    /// Class for InvokeFunctionActionOutput representing an object with properties.
    /// </summary>
    public class InvokeFunctionActionOutput<TBody> : MockOutput
    {
        /// <summary>
        /// The function's output.
        /// </summary>
        public TBody? Body { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="InvokeFunctionActionOutput"/> class.
        /// </summary>
        public InvokeFunctionActionOutput()
            : this(default)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InvokeFunctionActionOutput"/> class.
        /// </summary>
        /// <param name="body">The function's output</param>
        public InvokeFunctionActionOutput(TBody? body)
        {
            Body = body;
        }
    }
}