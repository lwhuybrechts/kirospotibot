using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telegram.Bot.Types;
using KiroSpotiBot.Core.Interfaces;
using KiroSpotiBot.Infrastructure.Options;

namespace KiroSpotiBot.Functions;

/// <summary>
/// Azure Function for handling Telegram webhook requests.
/// Acts as a thin controller that delegates to ITelegramUpdateHandler.
/// </summary>
public class TelegramWebhookFunction
{
    private readonly ILogger<TelegramWebhookFunction> _logger;
    private readonly ITelegramUpdateHandler _updateHandler;
    private readonly TelegramOptions _telegramOptions;

    public TelegramWebhookFunction(
        ILogger<TelegramWebhookFunction> logger,
        ITelegramUpdateHandler updateHandler,
        IOptions<TelegramOptions> telegramOptions)
    {
        _logger = logger;
        _updateHandler = updateHandler;
        _telegramOptions = telegramOptions.Value;
    }

    /// <summary>
    /// Handles incoming Telegram webhook requests.
    /// </summary>
    [Function("TelegramWebhook")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "webhook/telegram")] HttpRequest req,
        CancellationToken cancellationToken)
    {
        try
        {
            // Validate webhook signature if secret token is configured.
            if (!string.IsNullOrEmpty(_telegramOptions.WebhookSecretToken))
            {
                if (!ValidateWebhookSignature(req))
                {
                    _logger.LogWarning("Invalid webhook signature received.");
                    return new UnauthorizedResult();
                }
            }

            // Parse Telegram update from request body.
            Update? update;
            try
            {
                update = await req.ReadFromJsonAsync<Update>(cancellationToken);
                if (update == null)
                {
                    _logger.LogWarning("Received null update from Telegram webhook.");
                    return new BadRequestObjectResult(new { error = "Invalid update payload." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse Telegram update from request body.");
                return new BadRequestObjectResult(new { error = "Failed to parse update payload." });
            }

            // Delegate to handler for processing.
            await _updateHandler.HandleUpdateAsync(update, cancellationToken);

            // Always return 200 OK to prevent Telegram from retrying.
            return new OkResult();
        }
        catch (Exception ex)
        {
            // Log error to Sentry and return 200 OK to prevent retries.
            _logger.LogError(ex, "Unhandled error processing Telegram webhook.");
            return new OkResult();
        }
    }

    /// <summary>
    /// Validates the webhook signature using the secret token.
    /// </summary>
    private bool ValidateWebhookSignature(HttpRequest req)
    {
        if (!req.Headers.TryGetValue("X-Telegram-Bot-Api-Secret-Token", out var receivedToken))
        {
            return false;
        }

        return receivedToken.ToString() == _telegramOptions.WebhookSecretToken;
    }
}
