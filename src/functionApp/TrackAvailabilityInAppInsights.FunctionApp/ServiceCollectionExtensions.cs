using Azure.Identity;
using Azure.Monitor.OpenTelemetry.Exporter;

using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.Functions.Worker.OpenTelemetry;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using OpenTelemetry;
using OpenTelemetry.Resources;
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

        public static IServiceCollection RegisterTelemetryClient(this IServiceCollection services)
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

            return services;
        }
    }
}