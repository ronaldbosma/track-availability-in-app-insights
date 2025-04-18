namespace TrackAvailabilityInAppInsights.LogicApp.Functions
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.Azure.Functions.Extensions.Workflows;
    using Microsoft.Azure.Functions.Worker;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Represents the Functions flow invoked function.
    /// </summary>
    public class Functions
    {
        private readonly TelemetryClient _telemetryClient;
        private readonly ILogger<Functions> _logger;

        public Functions(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<Functions>();

            TelemetryConfiguration telemetryConfiguration = new()
            {
                ConnectionString = Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING"),
                TelemetryChannel = new InMemoryChannel()
            };
            _telemetryClient = new TelemetryClient(telemetryConfiguration); 
        }

        [Function("TrackAvailability")]
        public Task Run([WorkflowActionTrigger] string testName, bool success, DateTimeOffset startTime, string message)
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

            _telemetryClient.TrackAvailability(availability);
            _telemetryClient.Flush();

            return Task.CompletedTask;
        }
    }
}