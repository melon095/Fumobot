using System.Text.Json;
using Fumo.Shared.Exceptions;

namespace Fumo.Shared.ThirdParty.Emotes.SevenTV;

internal static class ExtractorHelpers
{
    /// <exception cref="InvalidInputException">
    /// If the user is not linked to Twitch.
    /// </exception>
    internal static JsonElement Connection(JsonElement data, string connectionKey = "connections")
    {
        var connection = data.GetProperty(connectionKey)
               .EnumerateArray()
               .FirstOrDefault(x => x.TryGetProperty("platform", out var platform) && platform.GetString() == "TWITCH");

        if (connection.ValueKind == JsonValueKind.Undefined)
            throw new InvalidInputException("user is not linked to Twitch");

        return connection;
    }
}
