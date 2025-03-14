﻿using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Fumo.Shared.ThirdParty.Emotes.SevenTV.Models;

[JsonConverter(typeof(Converter))]
public record SevenTVEmoteByName(ImmutableList<SevenTVEmoteByName.Item> Items)
{
    [JsonConverter(typeof(ItemConverter))]
    public class Item : ITagProperty
    {
        public string ID { get; init; }

        public string Name { get; init; }

        public Owner Owner { get; init; }

        public List<string> Tags { get; init; }

        public SevenTVBasicEmote AsBasicEmote() => new(ID, Name);
    }

    public record Owner(
        [property: JsonPropertyName("platformUsername")] string Username,
        [property: JsonPropertyName("platformId")] string TwitchID);

    internal class Converter : JsonConverter<SevenTVEmoteByName>
    {
        public override SevenTVEmoteByName? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var json = JsonDocument.ParseValue(ref reader).RootElement;
            var items = json
                .GetProperty("emotes")
                .GetProperty("search")
                .GetProperty("items")
                .Deserialize<List<Item>>(options)!
                .Select(x => x!)
                .ToImmutableList();

            return new(items);
        }

        public override void Write(Utf8JsonWriter writer, SevenTVEmoteByName value, JsonSerializerOptions options)
            => throw new NotImplementedException();
    }

    internal class ItemConverter : JsonConverter<Item>
    {
        public override Item? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var json = JsonDocument.ParseValue(ref reader).RootElement;

            var id = json.GetProperty("id").GetString()!;
            var name = json.GetProperty("defaultName").GetString()!;

            var owner = ExtractorHelpers.Connection(json.GetProperty("owner"))
                .Deserialize<Owner>(options)!;

            var tags = json
                .GetProperty("tags")
                .EnumerateArray()
                .Select(x => x.GetString()!)
                .ToList();

            return new()
            {
                ID = id,
                Name = name,
                Owner = owner,
                Tags = tags
            };
        }
        public override void Write(Utf8JsonWriter writer, Item value, JsonSerializerOptions options)
            => throw new NotImplementedException();
    }
}

public interface ITagProperty
{
    List<string> Tags { get; }
}
