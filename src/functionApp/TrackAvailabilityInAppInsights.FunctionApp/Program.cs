using Microsoft.Azure.Functions.Worker.Builder;

using TrackAvailabilityInAppInsights.FunctionApp;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Logging.AddOpenTelemetry(logging =>
{
    logging.IncludeFormattedMessage = true;
    logging.IncludeScopes = true;
});

builder.Services.ConfigureOpenTelemetry();
builder.Services.RegisterTelemetryClient();
builder.Services.RegisterDependencies();

builder.Build().Run();