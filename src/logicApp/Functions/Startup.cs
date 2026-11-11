using System;
using System.Diagnostics;

using Azure.Identity;

using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.Functions.Extensions.Workflows;
using Microsoft.Extensions.DependencyInjection;

namespace TrackAvailabilityInAppInsights.LogicApp.Functions;

public class Startup : IConfigureStartup
{
    private static ActivityListener s_workerActivityListener;

    /// <summary>
    /// Configures services for the Logic App custom .NET code project.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    public void Configure(IServiceCollection services)
    {
        RegisterTelemetryClient(services);
        AddActivityListener();
    }

    private static void RegisterTelemetryClient(IServiceCollection services)
    {
        TelemetryConfiguration telemetryConfiguration = new()
        {
            ConnectionString = Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING"),
            TelemetryChannel = new InMemoryChannel()
        };

        // Use Managed Identity to authenticate with Application Insights
        // See https://learn.microsoft.com/en-us/azure/azure-monitor/app/azure-ad-authentication for more details
        telemetryConfiguration.SetAzureTokenCredential(new ManagedIdentityCredential(new ManagedIdentityCredentialOptions()));

        TelemetryClient telemetryClient = new(telemetryConfiguration);

        services.AddSingleton(telemetryClient);
    }

    private static void AddActivityListener()
    {
        // Add an activity listener so we can correlate the availability telemetry and workflow logging.
        // The isolated worker only creates its per-invocation "Invoke" Activity (parented to the host's
        // traceparent) when a listener subscribes to its ActivitySource; otherwise Activity.Current is null.
        // The listener is stored in a static field so it stays alive for the lifetime of the worker.
        s_workerActivityListener = new ActivityListener
        {
            ShouldListenTo = source => source.Name.StartsWith("Microsoft.Azure.Functions.Worker", StringComparison.Ordinal),
            Sample = (ref _) => ActivitySamplingResult.AllData,
        };

        ActivitySource.AddActivityListener(s_workerActivityListener);
    }
}