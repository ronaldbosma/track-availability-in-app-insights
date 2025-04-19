namespace TrackAvailabilityInAppInsights.LogicApp.Functions
{
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.Azure.Functions.Extensions.Workflows;
    using Microsoft.Azure.Functions.Worker;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Contains functions related to availability tests.
    /// </summary>
    public class AvailabilityTestFunctions
    {
        private readonly TelemetryClient _telemetryClient;
        private readonly ILogger<AvailabilityTestFunctions> _logger;

        public AvailabilityTestFunctions(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<AvailabilityTestFunctions>();

            TelemetryConfiguration telemetryConfiguration = new()
            {
                ConnectionString = Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING"),
                TelemetryChannel = new InMemoryChannel()
            };
            _telemetryClient = new TelemetryClient(telemetryConfiguration); 
        }

        [Function("TrackIsAvailable")]
        public Task TrackIsAvailable([WorkflowActionTrigger] string testName, DateTimeOffset startTime)
        {
            _logger.LogInformation("TrackIsAvailable function invoked with testName: {TestName}, startTime: {StartTime}", testName, startTime);

            return TrackAvailability(testName, true, startTime, null);
        }

        [Function("TrackIsUnavailable")]
        public Task TrackIsUnavailable([WorkflowActionTrigger] string testName, DateTimeOffset startTime, string message)
        {
            _logger.LogInformation("TrackIsUnavailable function invoked with testName: {TestName}, startTime: {StartTime}, message: {Message}", testName, startTime, message);

            return TrackAvailability(testName, false, startTime, message);
        }

        public Task TrackAvailability([WorkflowActionTrigger] string testName, bool success, DateTimeOffset startTime, string message)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(testName, nameof(testName));

            AvailabilityTelemetry availability = new()
            {
                Name = testName,
                RunLocation = Environment.GetEnvironmentVariable("REGION_NAME") ?? "Unknown",
                Success = success,
                Message = message,
                Timestamp = startTime,
                Duration = DateTimeOffset.UtcNow - startTime
            };

            // Create activity to enable distributed tracing and correlation of the telemetry in App Insights
            using (Activity activity = new("AvailabilityContext"))
            {
                activity.Start();
                
                // Connect the availability telemetry to the logging activity
                availability.Id = activity.SpanId.ToString();
                availability.Context.Operation.ParentId = activity.ParentSpanId.ToString();
                availability.Context.Operation.Id = activity.RootId;
                
                _telemetryClient.TrackAvailability(availability);
                _telemetryClient.Flush();
            }
            
            return Task.CompletedTask;
        }
    }
}