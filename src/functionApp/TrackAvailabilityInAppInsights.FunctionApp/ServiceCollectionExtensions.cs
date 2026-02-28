using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using TrackAvailabilityInAppInsights.FunctionApp.AvailabilityTests;

namespace TrackAvailabilityInAppInsights.FunctionApp
{
    internal static class ServiceCollectionExtensions
    {
        public static IServiceCollection RegisterDependencies(this IServiceCollection services)
        {
            services.AddApplicationInsightsTelemetryWorkerService()
                    .ConfigureFunctionsApplicationInsights();

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
    }
}