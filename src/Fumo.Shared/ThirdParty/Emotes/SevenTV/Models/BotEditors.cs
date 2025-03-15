﻿using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Fumo.Shared.ThirdParty.Emotes.SevenTV.Models;

[JsonConverter(typeof(SevenTVBotEditors.UserConverter))]
public record SevenTVBotEditorUser(string ID, ImmutableList<string> EditorIDs);

[JsonConverter(typeof(Converter))]
public record SevenTVBotEditors(
    ImmutableList<SevenTVBotEditorUser> EditorOf
)
{
    internal class Converter : JsonConverter<SevenTVBotEditors>
    {
        public override SevenTVBotEditors? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var json = JsonDocument.ParseValue(ref reader).RootElement;
            var editorFor = json
                .GetProperty("users")
                .GetProperty("userByConnection")
                .GetProperty("editorFor")
                .Deserialize<List<SevenTVBotEditorUser>>(options)!
                .Select(x => x!)
                .ToImmutableList()!;

            return new(editorFor);
        }

        public override void Write(Utf8JsonWriter writer, SevenTVBotEditors value, JsonSerializerOptions options)
            => throw new NotImplementedException();
    }

    internal class UserConverter : JsonConverter<SevenTVBotEditorUser>
    {
        public override SevenTVBotEditorUser? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var json = JsonDocument.ParseValue(ref reader)
                .RootElement
                .GetProperty("user");

            var id = ExtractorHelpers.Connection(json)
                .TryGetProperty("platformId", out var platformId)
                ? platformId.GetString()!
                : throw SevenTVErrors.NotLinkedToTwitch();

            var editorIDs = json
                .GetProperty("editors")
                .EnumerateArray()
                .Select((x) => ExtractorHelpers.Connection(x.GetProperty("editor")))
                .Select(x => x.ValueKind != JsonValueKind.Null
                             ? x.GetProperty("platformId").GetString()
                             : null)
                .Select(x => x!)
                .ToImmutableList();

            return new(id, editorIDs);
        }

        public override void Write(Utf8JsonWriter writer, SevenTVBotEditorUser value, JsonSerializerOptions options)
            => throw new NotImplementedException();
    }
}
