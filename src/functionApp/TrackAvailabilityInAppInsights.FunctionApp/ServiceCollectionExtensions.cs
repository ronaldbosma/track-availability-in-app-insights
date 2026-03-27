using Azure.Identity;
using Azure.Monitor.OpenTelemetry.Exporter;

using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.Functions.Worker.OpenTelemetry;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using OpenTelemetry.Trace;

using TrackAvailabilityInAppInsights.FunctionApp.AvailabilityTests;

namespace TrackAvailabilityInAppInsights.FunctionApp
{
    internal static class ServiceCollectionExtensions
    {
        public static IServiceCollection ConfigureOpenTelemetry(this IServiceCollection services)
        {
            services.AddOpenTelemetry()
                .WithTracing(tracing => tracing
                    // Enables HttpClient instrumentation.
                    .AddHttpClientInstrumentation());

            services.AddOpenTelemetry().UseAzureMonitorExporter(options =>
            {
                // Use the system-assigned managed identity to authenticate to Azure Monitor.
                // See https://learn.microsoft.com/en-us/azure/azure-monitor/app/azure-ad-authentication for more details.
                options.Credential = new ManagedIdentityCredential(new ManagedIdentityCredentialOptions());
            });

            services.AddOpenTelemetry().UseFunctionsWorkerDefaults();

            return services;
        }

        public static IServiceCollection RegisterDependencies(this IServiceCollection services)
        {
            services.RegisterTelemetryClient();

            services.AddOptionsWithValidateOnStart<ApiManagementOptions>()
                    .BindConfiguration(ApiManagementOptions.SectionKey)
                    .ValidateDataAnnotations();

            services.AddSingleton<IAvailabilityTestFactory, AvailabilityTestFactory>();
            services.AddSingleton<SslCertificateValidator>();

            services.AddHttpClient("apim", (sp, client) =>
            {
                var options = sp.GetRequiredService<IOptions<ApiManagementOptions>>().Value;

                client.BaseAddress = new Uri(options.GatewayUrl);
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", options.SubscriptionKey);
            });

            return services;
        }

        private static IServiceCollection RegisterTelemetryClient(this IServiceCollection services)
        {
            var telemetryConfiguration = TelemetryConfiguration.CreateDefault();
            telemetryConfiguration.ConnectionString = Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING");

            // Use the system-assigned managed identity to authenticate to Azure Monitor.
            // See https://learn.microsoft.com/en-us/azure/azure-monitor/app/azure-ad-authentication for more details.
            telemetryConfiguration.SetAzureTokenCredential(new ManagedIdentityCredential(new ManagedIdentityCredentialOptions()));

            TelemetryClient telemetryClient = new(telemetryConfiguration);

            services.AddSingleton(telemetryClient);

            return services;
        }
    }
}