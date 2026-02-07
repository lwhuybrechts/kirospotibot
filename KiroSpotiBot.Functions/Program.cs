using Azure.Data.Tables;
using KiroSpotiBot.Infrastructure.Repositories;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
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

// Configure Azure Table Storage
var storageConnectionString = builder.Configuration["AZURE_STORAGE_CONNECTION_STRING"];
if (!string.IsNullOrEmpty(storageConnectionString))
{
    var tableServiceClient = new TableServiceClient(storageConnectionString);
    builder.Services.AddSingleton(tableServiceClient);
    
    // Register repositories
    builder.Services.AddSingleton(typeof(IRepository<>), typeof(BaseRepository<>));
}

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Build().Run();
