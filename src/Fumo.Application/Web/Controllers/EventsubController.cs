using System.Text;
using System.Text.Json;
using Fumo.Shared.Eventsub;
using Fumo.Shared.Eventsub.Commands;
using Fumo.Shared.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Fumo.Application.Web.Controllers;

[Route("api/[controller]")]
[ApiController]
public class EventsubController : ControllerBase
{
    private const string HeaderMessageID = "Twitch-Eventsub-Message-Id";
    private const string HeaderMessageType = "Twitch-Eventsub-Message-Type";
    private const string HeaderMessageSignature = "Twitch-Eventsub-Message-Signature";
    private const string HeaderMessageTimestamp = "Twitch-Eventsub-Message-Timestamp";

    private const string MessageTypeVerification = "webhook_callback_verification";
    private const string MessageTypeNotification = "notification";
    private const string MessageTypeRevocation = "revocation";

    private readonly IEventsubManager EventsubManager;
    private readonly Serilog.ILogger Logger;
    private readonly IMediator Bus;

    public EventsubController(IEventsubManager eventsubManager, IMediator bus, Serilog.ILogger logger)
    {
        EventsubManager = eventsubManager;
        Bus = bus;
        Logger = logger.ForContext<EventsubController>();
    }


    [HttpPost("Callback")]
    public async ValueTask<IActionResult> Callback(CancellationToken ct)
    {
        string messageId = Request.Headers[HeaderMessageID].ToString(),
            messageType = Request.Headers[HeaderMessageType].ToString(),
            messageTimestamp = Request.Headers[HeaderMessageTimestamp].ToString(),
            messageSignature = Request.Headers[HeaderMessageSignature].ToString();

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

        switch (messageType)
        {
            case MessageTypeVerification:
                {
                    var verification = JsonSerializer.Deserialize<MessageTypeVerificationBody>(body, FumoJson.SnakeCase)!;

                    await Bus.Send(new EventsubVerificationCommand(verification.Subscription), ct);

                    return Ok(verification.Challenge);
                }

            case MessageTypeRevocation:
                {
                    var revocation = JsonSerializer.Deserialize<MessageTypeRevocationBody>(body, FumoJson.SnakeCase)!;

                    await Bus.Send(new EventsubRevocationCommand(revocation.Subscription), ct);
                }
                break;

            case MessageTypeNotification:
                {
                    var notification = JsonSerializer.Deserialize<MessageTypeNotificationBody>(body, FumoJson.SnakeCase)!;

                    await Bus.Send(new EventsubNotificationCommand(notification.Subscription, notification.Event), ct);
                }
                break;

            default:
                {
                    Logger.Warning("Unknown message type {MessageType}", messageType);
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
