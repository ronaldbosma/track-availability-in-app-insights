using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Configuration;
using TrackAvailabilityInAppInsights.FunctionApp.AvailabilityTests;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services.AddApplicationInsightsTelemetryWorkerService()
                .ConfigureFunctionsApplicationInsights();

builder.Services.AddSingleton<IAvailabilityTestFactory, AvailabilityTestFactory>();

builder.Services.AddHttpClient("apim", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiManagement_gatewayUrl"] ?? throw new ConfigurationErrorsException("Setting ApiManagement_gatewayUrl not specified"));
    client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", builder.Configuration["ApiManagement_subscriptionKey"]);
});

builder.Build().Run();
