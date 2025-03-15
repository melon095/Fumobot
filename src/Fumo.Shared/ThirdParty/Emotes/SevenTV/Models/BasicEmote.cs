using System.Text.Json;
using System.Text.Json.Serialization;

namespace Fumo.Shared.ThirdParty.Emotes.SevenTV.Models;

[JsonConverter(typeof(Converter))]
public record SevenTVBasicEmote(string ID, string Name)
{
    internal class Converter : JsonConverter<SevenTVBasicEmote>
    {
        public override SevenTVBasicEmote? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var json = JsonDocument.ParseValue(ref reader).RootElement;

            var id = json.GetProperty("id").GetString()!;
            var name = json.TryGetProperty("defaultName", out var defaultName)
                ? defaultName.GetString()!
                : json.GetProperty("alias").GetString();

            return new SevenTVBasicEmote(id, name!);
        }

        public override void Write(Utf8JsonWriter writer, SevenTVBasicEmote value, JsonSerializerOptions options)
            => throw new NotImplementedException();
    }
}
