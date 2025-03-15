using System.Text.Json;

namespace Fumo.Shared.ThirdParty.Emotes.SevenTV;

internal static class ExtractorHelpers
{
    internal static JsonElement Connection(JsonElement data, string connectionKey = "connections")
    {
        var connection = data.GetProperty(connectionKey)
               .EnumerateArray()
               .FirstOrDefault(x => x.TryGetProperty("platform", out var platform) &&
                                    platform.GetString() == "TWITCH");

        return connection;
    }
}
