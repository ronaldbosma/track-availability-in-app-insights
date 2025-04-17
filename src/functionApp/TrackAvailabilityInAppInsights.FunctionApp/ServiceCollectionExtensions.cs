using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Configuration;
using TrackAvailabilityInAppInsights.FunctionApp.AvailabilityTests;

namespace TrackAvailabilityInAppInsights.FunctionApp
{
    internal static class ServiceCollectionExtensions
    {
        public static IServiceCollection RegisterDependencies(this IServiceCollection services, IConfigurationManager configuration)
        {
            services.AddApplicationInsightsTelemetryWorkerService()
                    .ConfigureFunctionsApplicationInsights();

            services.AddSingleton<IAvailabilityTestFactory, AvailabilityTestFactory>();
            services.AddSingleton<SslCertificateValidator>();

            services.AddHttpClient("apim", client =>
            {
                client.BaseAddress = new Uri(configuration["ApiManagement_gatewayUrl"] ?? throw new ConfigurationErrorsException("Setting ApiManagement_gatewayUrl not specified"));
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", configuration["ApiManagement_subscriptionKey"]);
            });

            return services;
        }
    }
}
