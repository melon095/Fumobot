
using Fumo.Shared.Interfaces;
using Fumo.Models;
using StackExchange.Redis;

namespace Fumo.Handlers;

public class CooldownHandler : ICooldownHandler
{
    private readonly IDatabase Redis;

    public CooldownHandler(IDatabase redis)
    {
        Redis = redis;
    }

    private static string Key(ChatMessage message, ChatCommand command) => $"channel:{message.Channel.TwitchID}:cooldown:{command.NameMatcher}:{message.User.TwitchID}";


    public async Task<bool> IsOnCooldownAsync(ChatMessage message, ChatCommand command)
    {
        var key = Key(message, command);

        return (await this.Redis.KeyExistsAsync(key)) == true;
    }

    public async Task SetCooldownAsync(ChatMessage message, ChatCommand command)
    {
        var key = Key(message, command);

        await this.Redis.StringSetAsync(key, 1, expiry: command.Cooldown);
    }
}
