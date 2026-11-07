using System;

using Azure.Identity;

using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.Functions.Extensions.Workflows;
using Microsoft.Extensions.DependencyInjection;

using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace TrackAvailabilityInAppInsights.LogicApp.Functions;

public class Startup : IConfigureStartup
{
    /// <summary>
    /// Configures services for the Logic App custom .NET code project.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    public void Configure(IServiceCollection services)
    {
        var telemetryConfiguration = TelemetryConfiguration.CreateDefault();
        telemetryConfiguration.ConnectionString = Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING");

        // Configure the Cloud.RoleName, Cloud.RoleInstance and Component.Version so their set on the Availability Test telemetry
        telemetryConfiguration.ConfigureOpenTelemetryBuilder(builder => builder
            .ConfigureResource(r => r
                .AddService(
                    serviceName: Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME") ?? Environment.MachineName,
                    serviceInstanceId: Environment.MachineName,
                    serviceVersion: "1.0.0.0"
                )
            ));

        // Use the system-assigned managed identity to authenticate to Azure Monitor.
        // See https://learn.microsoft.com/en-us/azure/azure-monitor/app/azure-ad-authentication for more details.
        telemetryConfiguration.SetAzureTokenCredential(new ManagedIdentityCredential(new ManagedIdentityCredentialOptions()));

        TelemetryClient telemetryClient = new(telemetryConfiguration);

        services.AddSingleton(telemetryClient);
    }
}