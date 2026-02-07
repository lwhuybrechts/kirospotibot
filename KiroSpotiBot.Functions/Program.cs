using KiroSpotiBot.Infrastructure;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sentry.Extensions.Logging;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// Configure Sentry logging
var sentryDsn = builder.Configuration["SENTRY_DSN"];
builder.Services.AddLogging(logging =>
{
    if (!string.IsNullOrEmpty(sentryDsn))
    {
        logging.AddSentry(options =>
        {
            options.Dsn = sentryDsn;
            options.Debug = builder.Configuration["SENTRY_ENVIRONMENT"] == "development";
        });
    }
});

// Register infrastructure services (repositories, encryption, Azure Table Storage)
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Build().Run();
