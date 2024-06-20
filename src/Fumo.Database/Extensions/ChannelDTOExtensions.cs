using Fumo.Database.DTO;

namespace Fumo.Database.Extensions;

public static class ChannelDTOExtensions
{
    public static string GetSetting(this ChannelDTO channel, string key)
    {
        return channel.Settings.FirstOrDefault(x => x.Key == key)?.Value ?? string.Empty;
    }

    public static string SetSetting(this ChannelDTO channel, string key, string value)
    {
        var setting = channel.Settings.FirstOrDefault(x => x.Key == key);
        if (setting is null)
        {
            Setting newSetting = new()
            {
                Key = key,
                Value = value
            };

            channel.Settings.Add(newSetting);
            return value;
        }
        setting.Value = value;
        return value;
    }

    public static void RemoveSetting(this ChannelDTO channel, string key)
    {
        var setting = channel.Settings.FirstOrDefault(x => x.Key == key);
        if (setting is not null)
        {
            channel.Settings.Remove(setting);
        }
    }
}
