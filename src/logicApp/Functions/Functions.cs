//------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------

namespace TrackAvailabilityInAppInsights.LogicApp.Functions
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.DataContracts;
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

        public Functions(TelemetryClient telemetryClient, ILoggerFactory loggerFactory)
        {
            _telemetryClient = telemetryClient;
            _logger = loggerFactory.CreateLogger<Functions>();

            // TelemetryConfiguration telemetryConfiguration = new(); 
            // telemetryConfiguration.ConnectionString = Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING"); 
            // telemetryConfiguration.TelemetryChannel = new InMemoryChannel(); 
            // _telemetryClient = new TelemetryClient(telemetryConfiguration); 
        }

        [Function("TrackAvailability")]
        public Task Run([WorkflowActionTrigger] string testName, bool success, string message)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(testName, nameof(testName));

            AvailabilityTelemetry availability = new()
            {
                Name = testName,
                RunLocation = Environment.GetEnvironmentVariable("REGION_NAME") ?? "Unknown",
                Success = success,
                Message = message
            };

            _telemetryClient.TrackAvailability(availability);
            _telemetryClient.Flush();

            return Task.CompletedTask;
        }
    }
}