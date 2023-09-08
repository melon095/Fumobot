﻿using Fumo.Database;
using Fumo.Database.DTO;
using Fumo.Database.Extensions;
using Fumo.Exceptions;
using Fumo.ThirdParty.Emotes.SevenTV;
using StackExchange.Redis;
using System.Threading.Channels;

namespace Fumo.Extensions;

// TODO: Im not entirely sure if this is the best way of doing this, just thinking of that the Fumo.ThirdParty library shouldn't know of redis and whatnot...
internal static class SevenTVEnsurePermissionsExtensions
{
    public static string EditorKey(this ISevenTVService _, string channelID) => $"channel:{channelID}:seventv:editors";

    /// <summary>
    /// Ensures the current user is allowed to change emotes in the channel
    /// </summary>
    public static async Task<(string EmoteSet, string UserID)> EnsureCanModify(this ISevenTVService service, string botID, IDatabase redis, ChannelDTO channel, UserDTO invoker)
    {
        var currentEmoteSet = channel.GetSetting(ChannelSettingKey.SevenTV_EmoteSet)
            ?? throw new InvalidInputException("The channel is missing an emote set");

        // Is broadcaster
        if (channel.TwitchID == invoker.TwitchID)
        {
            return (currentEmoteSet, channel.TwitchID);
        }

        RedisValue[] redisValues = new[] { new RedisValue(botID), new RedisValue(invoker.TwitchID) };
        var contains = await redis.SetContainsAsync(service.EditorKey(channel.TwitchID), redisValues);

        if (contains.ElementAt(0) == false)
        {
            throw new InvalidInputException("I am not an editor in this channel");
        }

        if (contains.ElementAt(1) == false)
        {
            throw new InvalidInputException("You're not an editor in this channel");
        }

        return (currentEmoteSet, invoker.TwitchID);
    }
}