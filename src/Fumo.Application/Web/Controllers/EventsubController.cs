﻿using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Fumo.Shared.Eventsub;
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
    private const string HeaderSubscriptionventType = "Twitch-Eventsub-Subscription-Type";

    private const string MessageTypeVerification = "webhook_callback_verification";
    private const string MessageTypeNotification = "notification";
    private const string MessageTypeRevocation = "revocation";

    private readonly IEventsubManager EventsubManager;
    private readonly Serilog.ILogger Logger;
    private readonly IMediator Bus;
    private readonly IEventsubCommandFactory EventsubCommandFactory;

    public EventsubController(IEventsubManager eventsubManager, IMediator bus, Serilog.ILogger logger, IEventsubCommandFactory eventsubCommandFactory)
    {
        EventsubManager = eventsubManager;
        Bus = bus;
        Logger = logger.ForContext<EventsubController>();
        EventsubCommandFactory = eventsubCommandFactory;
    }

    [HttpPost("Callback")]
    public async ValueTask<IActionResult> Callback(CancellationToken ct)
    {
        string messageId = Request.Headers[HeaderMessageID].ToString(),
            messageType = Request.Headers[HeaderMessageType].ToString(),
            messageTimestamp = Request.Headers[HeaderMessageTimestamp].ToString(),
            messageSignature = Request.Headers[HeaderMessageSignature].ToString(),
            type = Request.Headers[HeaderSubscriptionventType].ToString();

        if (string.IsNullOrEmpty(messageId) ||
            string.IsNullOrEmpty(messageType) ||
            string.IsNullOrEmpty(messageTimestamp) ||
            string.IsNullOrEmpty(messageSignature))
        {
            return Unauthorized("Missing Headers");
        }

        var secret = await EventsubManager.GetSecret();
        var body = await ReadBody();

        var hmacBuilder = new StringBuilder();
        hmacBuilder.Append(messageId);
        hmacBuilder.Append(messageTimestamp);
        hmacBuilder.Append(body);

        if (CheckSignature(hmacBuilder.ToString(), messageSignature, secret) is false)
        {
            return Unauthorized("Invalid Signature");
        }

        var jsonBody = JsonSerializer.Deserialize<JsonElement>(body, FumoJson.SnakeCase)!;

        switch (messageType)
        {
            case MessageTypeVerification:
                {
                    var challenge = jsonBody.GetProperty("challenge").GetString()!;

                    var command = EventsubCommandFactory.Create(EventsubCommandType.Verification, type, jsonBody);
                    if (command is not null)
                        await Bus.Send(command, ct);

                    return Ok(challenge);
                }

            case MessageTypeRevocation:
                {
                    var command = EventsubCommandFactory.Create(EventsubCommandType.Revocation, type, jsonBody);
                    if (command is not null)
                        await Bus.Send(command, ct);
                }
                break;

            case MessageTypeNotification:
                {
                    var msgEvent = jsonBody.GetProperty("event");
                    var command = EventsubCommandFactory.Create(EventsubCommandType.Notification, type, msgEvent);
                    if (command is not null)
                        await Bus.Send(command, ct);
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

    private static bool CheckSignature(string message, string signature, string secret)
    {
        var messageBytes = Encoding.UTF8.GetBytes(message);
        var signatureBytes = Encoding.UTF8.GetBytes(signature);

        using HMACSHA256 hmacGen = new(Encoding.UTF8.GetBytes(secret));
        var computedHash = hmacGen.ComputeHash(messageBytes);
        var finalHmac = $"sha256={BitConverter.ToString(computedHash).Replace("-", "").ToLower()}";
        var finalBytes = Encoding.UTF8.GetBytes(finalHmac);

        return CryptographicOperations.FixedTimeEquals(finalBytes, signatureBytes);
    }
}
