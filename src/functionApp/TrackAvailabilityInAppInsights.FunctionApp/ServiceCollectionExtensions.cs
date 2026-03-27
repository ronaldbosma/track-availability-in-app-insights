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
                // Set the Azure Monitor credential to the DefaultAzureCredential.
                // This credential will use the Azure identity of the current user or
                // the service principal that the application is running as to authenticate
                // to Azure Monitor.
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

            return services;
        }
    }
}