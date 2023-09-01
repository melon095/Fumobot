using System.Text.Json.Serialization;

namespace Fumo.ThirdParty.Pajbot1;

public record BanphraseData(
    [property: JsonPropertyName("case_sensitive")] bool CaseSensitive,
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("length")] int Length,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("operator")] string Operator,
    [property: JsonPropertyName("permanent")] bool Permanent,
    [property: JsonPropertyName("phrase")] string Phrase,
    [property: JsonPropertyName("remove_accents")] bool RemoveAccents,
    [property: JsonPropertyName("sub_immunity")] bool SubImmunity
);

public record PajbotResponse(
    [property: JsonPropertyName("banned")] bool Banned,
    [property: JsonPropertyName("banphrase_data")] BanphraseData BanphraseData,
    [property: JsonPropertyName("input_message")] string InputMessage
);

