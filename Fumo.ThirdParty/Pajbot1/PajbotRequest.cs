using System.Text.Json.Serialization;

namespace Fumo.ThirdParty.Pajbot1;

internal record PajbotRequest([property: JsonPropertyName("message")] string Message);
