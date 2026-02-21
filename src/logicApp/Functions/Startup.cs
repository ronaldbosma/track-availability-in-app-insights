namespace TrackAvailabilityInAppInsights.LogicApp.Functions
{
    using System;
    using Azure.Identity;
    using Microsoft.Azure.Functions.Extensions.Workflows;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility;

    public class Startup : IConfigureStartup
    {
        /// <summary>
        /// Configures services for the Logic App custom .NET code project.
        /// </summary>
        /// <param name="services">The service collection to configure.</param>
        public void Configure(IServiceCollection services)
        {
            TelemetryConfiguration telemetryConfiguration = new()
            {
                ConnectionString = Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING"),
                TelemetryChannel = new InMemoryChannel()
            };
            telemetryConfiguration.SetAzureTokenCredential(new ManagedIdentityCredential());

            TelemetryClient telemetryClient = new(telemetryConfiguration); 

            services.AddSingleton(telemetryClient);
        }
    }
}