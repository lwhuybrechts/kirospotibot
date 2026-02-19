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
            _logger.LogInformation("Received Telegram webhook request.");

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
            // Use Telegram.Bot's JsonBotAPI.Options for correct snake_case deserialization.
            Update? update;
            try
            {
                using var reader = new StreamReader(req.Body);
                var body = await reader.ReadToEndAsync(cancellationToken);
                
                _logger.LogInformation("Parsing webhook payload. Length: {BodyLength}", body.Length);
                
                update = System.Text.Json.JsonSerializer.Deserialize<Update>(body, Telegram.Bot.JsonBotAPI.Options);
                
                if (update == null)
                {
                    _logger.LogWarning("Received null update from Telegram webhook.");
                    return new BadRequestObjectResult(new { error = "Invalid update payload." });
                }
                
                _logger.LogInformation(
                    "Successfully parsed update. UpdateId: {UpdateId}, Type: {UpdateType}",
                    update.Id,
                    update.Type);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to parse Telegram update from request body. Error: {ErrorMessage}",
                    ex.Message);
                return new BadRequestObjectResult(new { error = "Failed to parse update payload." });
            }

            // Delegate to handler for processing.
            await _updateHandler.HandleUpdateAsync(update, cancellationToken);

            _logger.LogInformation("Successfully processed webhook request.");

            // Always return 200 OK to prevent Telegram from retrying.
            return new OkResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error processing Telegram webhook.");
            
            // Return 200 OK to prevent Telegram from retrying.
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
