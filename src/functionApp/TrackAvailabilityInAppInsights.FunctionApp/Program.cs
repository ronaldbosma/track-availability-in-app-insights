using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Hosting;
using TrackAvailabilityInAppInsights.FunctionApp;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services.RegisterDependencies(builder.Configuration);

builder.Build().Run();
