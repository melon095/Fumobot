using Fumo.Shared.Mediator;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using SerilogTracing;

namespace Fumo.Application.Web;

public static class ChatDebuggerEndpoint
{
    public static void MapChatDebuggerEndpoint(this WebApplication app)
    {
#if !DEBUG
        return;
#else
        Log.Information("Chat Debugger endpoint -> /chat-debugger");
        app.MapPost("/chat-debugger", async (
            [FromBody] ChatMessageNotificationCommand message,
            [FromServices] Serilog.ILogger rLogger,
            [FromServices] IMediator bus,
            [FromServices] CancellationToken token) =>
        {
            var log = rLogger.ForContext("SourceContext", nameof(ChatDebuggerEndpoint));
            using var activity = log.StartActivity("Chat Debugger");

            await bus.Publish(message, token).ConfigureAwait(false);
        });
#endif
    }
}