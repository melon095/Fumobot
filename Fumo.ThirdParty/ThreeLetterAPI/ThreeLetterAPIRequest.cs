using System.Text.Json.Serialization;

namespace Fumo.ThirdParty.ThreeLetterAPI;

public class ThreeLetterAPIRequest
{
    [JsonPropertyName("operationName")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? OperationName { get; set; }

    [JsonPropertyName("extensions")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Extension? Extensions { get; set; }

    [JsonPropertyName("query")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Query { get; set; }

    [JsonPropertyName("variables")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Variables { get; set; }
}