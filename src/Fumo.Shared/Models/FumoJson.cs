using System.Text.Json;

namespace Fumo.Shared.Models;

public static class FumoJson
{
    public static JsonSerializerOptions CamelCase => new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static JsonSerializerOptions SnakeCase => new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };
}
