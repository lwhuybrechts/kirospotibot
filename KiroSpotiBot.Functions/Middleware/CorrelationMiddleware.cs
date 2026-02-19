using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;

namespace KiroSpotiBot.Functions.Middleware;

/// <summary>
/// Middleware for managing correlation IDs across Azure Function invocations.
/// Extracts correlation ID from request headers or generates a new one.
/// </summary>
public class CorrelationMiddleware : IFunctionsWorkerMiddleware
{
    private readonly ILogger<CorrelationMiddleware> _logger;

    public CorrelationMiddleware(ILogger<CorrelationMiddleware> logger)
    {
        _logger = logger;
    }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var httpRequestData = await context.GetHttpRequestDataAsync();
        string? correlationId = null;

        // Try to extract correlation ID from request headers.
        if (httpRequestData != null &&
            httpRequestData.Headers.TryGetValues("x-correlation-id", out var values))
        {
            correlationId = values.FirstOrDefault();
        }

        // Generate new correlation ID if not provided.
        correlationId ??= Guid.NewGuid().ToString("N");

        // Use logging scope to add correlation ID to all log entries automatically.
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId
        }))
        {
            // Store correlation ID in function context for potential use.
            context.Items["CorrelationId"] = correlationId;

            _logger.LogDebug("Processing function {FunctionName} with correlation ID {CorrelationId}.",
                context.FunctionDefinition.Name, correlationId);

            // Execute the function.
            await next(context);
        }
    }
}
