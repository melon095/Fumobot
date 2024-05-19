using System.Text;
using System.Text.Json;
using Fumo.Shared.Eventsub;
using Microsoft.AspNetCore.Mvc;

namespace Fumo.Application.Web.Controllers;

[Route("api/[controller]")]
[ApiController]
public class EventsubController : ControllerBase
{
    // TODO: Remove this
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    private const string HeaderMessageID = "Twitch-Eventsub-Message-Id";
    private const string HeaderMessageType = "Twitch-Eventsub-Message-Type";
    private const string HeaderMessageSignature = "Twitch-Eventsub-Message-Signature";
    private const string HeaderMessageTimestamp = "Twitch-Eventsub-Message-Timestamp";

    private const string MessageTypeVerification = "webhook_callback_verification";
    private const string MessageTypeNotification = "notification";
    private const string MessageTypeRevocation = "revocation";

    private readonly IEventsubManager EventsubManager;
    private readonly Serilog.ILogger Logger;

    public EventsubController(IEventsubManager eventsubManager, Serilog.ILogger logger)
    {
        EventsubManager = eventsubManager;
        Logger = logger.ForContext<EventsubController>();
    }

    [HttpPost("Callback")]
    public async ValueTask<IActionResult> Callback(CancellationToken ct)
    {
        var messageId = Request.Headers[HeaderMessageID].ToString();
        var messageType = Request.Headers[HeaderMessageType].ToString();
        var messageTimestamp = Request.Headers[HeaderMessageTimestamp].ToString();
        var messageSignature = Request.Headers[HeaderMessageSignature].ToString();

        if (string.IsNullOrEmpty(messageId) ||
            string.IsNullOrEmpty(messageType) ||
            string.IsNullOrEmpty(messageTimestamp) ||
            string.IsNullOrEmpty(messageSignature))
        {
            return Unauthorized("Missing Headers");
        }

        var body = await ReadBody();

        var hmacBuilder = new StringBuilder();
        hmacBuilder.Append(messageId);
        hmacBuilder.Append(messageTimestamp);
        hmacBuilder.Append(body);

        var signatureMatch = await EventsubManager.CheckSignature(hmacBuilder.ToString(), messageSignature);

        if (!signatureMatch)
        {
            Logger.Warning("Invalid Signature");
            return Unauthorized("Invalid Signature");
        }

        Logger.Information("Received {MessageType} message", messageType);

        switch (messageType)
        {
            case MessageTypeVerification:
                {
                    var verification = JsonSerializer.Deserialize<MessageTypeVerificationBody>(body, SerializerOptions)!;

                    return Ok(verification.Challenge);
                }

            case MessageTypeRevocation:
                {
                    var revocation = JsonSerializer.Deserialize<MessageTypeRevocationBody>(body, SerializerOptions)!;

                    await EventsubManager.HandleMessage(revocation, ct);
                }
                break;

            case MessageTypeNotification:
                {
                    var notification = JsonSerializer.Deserialize<MessageTypeNotificationBody>(body, SerializerOptions)!;

                    await EventsubManager.HandleMessage(notification, ct);
                }
                break;
        }

        return NoContent();
    }

    private async Task<string> ReadBody()
    {
        using StreamReader reader = new(Request.Body);

        return await reader.ReadToEndAsync();
    }
}
