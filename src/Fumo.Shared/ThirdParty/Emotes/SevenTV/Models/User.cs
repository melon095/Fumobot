using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Fumo.Shared.ThirdParty.Emotes.SevenTV.Models;

public record SevenTVUserEmote(string ID, string Alias);

public record SevenTVUserEmoteSet(string ID, IReadOnlyList<SevenTVUserEmote> Emotes, int Capacity);

[JsonConverter(typeof(Converter))]
public record SevenTVUser(
    string SevenTVID,
    string TwitchID,
    string Username,
    ImmutableList<string> Roles,
    DateTime CreatedAt,
    SevenTVUserEmoteSet? EmoteSet
)
{
    public bool IsDeletedUser()
        => Username == "*DeletedUser" || SevenTVID.All(x => x == '0');

    internal class Converter : JsonConverter<SevenTVUser>
    {
        public override SevenTVUser Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var user = JsonDocument.ParseValue(ref reader)
                .RootElement
                .GetProperty("users")
                .GetProperty("userByConnection");

            if (user.ValueKind == JsonValueKind.Null)
                throw new JsonException("User not found");

            var connection = ExtractorHelpers.Connection(user);

            var id = user.GetProperty("id").GetString()!;
            var username = connection.GetProperty("platformUsername").GetString()!;
            var twitchId = connection.GetProperty("platformId").GetString()!;

            var roles = user
                .GetProperty("roles")
                .EnumerateArray()
                .Select(x => x.TryGetProperty("name", out var name) ? name.GetString() : string.Empty)
                .ToImmutableList();

            //var createdAt = user.GetProperty("createdAt").Deserialize<DateTime>(options)!;
            var createdAt = DateTime.Now; // TODO: Not supported atm.
            SevenTVUserEmoteSet? emoteSet = null;

            if (user.GetProperty("style").TryGetProperty("activeEmoteSet", out var set) &&
                set.ValueKind != JsonValueKind.Null)
            {
                emoteSet = new SevenTVUserEmoteSet(
                    set.GetProperty("id").GetString()!,
                    set.GetProperty("emotes")
                        .GetProperty("items")
                        .Deserialize<List<SevenTVUserEmote>>(options)!
                        .Select(x => x!)
                        .ToList(),
                    set.GetProperty("capacity").GetInt32()
                );
            }

            return new(
                id,
                twitchId,
                username,
                roles!,
                createdAt,
                emoteSet
            );
        }
        public override void Write(Utf8JsonWriter writer, SevenTVUser value, JsonSerializerOptions options)
            => throw new NotImplementedException();
    }
}
