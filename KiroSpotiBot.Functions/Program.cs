using KiroSpotiBot.Functions.Middleware;
using KiroSpotiBot.Infrastructure;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sentry.Extensions.Logging;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureFunctionsWebApplication(workerApplication =>
    {
        // Register correlation ID middleware to run before all functions.
        workerApplication.UseMiddleware<CorrelationMiddleware>();
    })
    .ConfigureServices((context, services) =>
    {
        // Configure Sentry logging
        var sentryDsn = context.Configuration["SENTRY_DSN"];
        services.AddLogging(logging =>
        {
            if (!string.IsNullOrEmpty(sentryDsn))
            {
                logging.AddSentry(options =>
                {
                    options.Dsn = sentryDsn;
                    options.Debug = context.Configuration["SENTRY_ENVIRONMENT"] == "development";
                });
            }
        });

        // Register infrastructure services (repositories, encryption, Azure Table Storage)
        services.AddInfrastructure(context.Configuration);

        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
    });

builder.Build().Run();
