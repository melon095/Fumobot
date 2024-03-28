using Fumo.Shared.Interfaces;
using Fumo.Shared.Models;
using StackExchange.Redis;

namespace Fumo.Application.Bot.Handlers;

public class CooldownHandler : ICooldownHandler
{
    private readonly IDatabase Redis;

    public CooldownHandler(IDatabase redis)
    {
        Redis = redis;
    }

    private static string Key(ChatMessage message, ChatCommand command) => $"channel:{message.Channel.TwitchID}:cooldown:{command.NameMatcher}:{message.User.TwitchID}";


    public async ValueTask<bool> IsOnCooldown(ChatMessage message, ChatCommand command)
    {
        var key = Key(message, command);

        return await Redis.KeyExistsAsync(key);
    }

    public async ValueTask SetCooldown(ChatMessage message, ChatCommand command)
    {
        var key = Key(message, command);

        await Redis.StringSetAsync(key, 1, expiry: command.Cooldown);
    }
}
