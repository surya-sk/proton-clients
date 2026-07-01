using System.Text.Json;
using System.Text.Json.Serialization;

namespace Proton.Core.Http;

/// <summary>Mirrors go-proton-api's APIError body shape returned on non-2xx responses.</summary>
public sealed class ApiErrorResponse
{
    [JsonPropertyName("Code")]
    public int Code { get; set; }

    [JsonPropertyName("Error")]
    public string Error { get; set; } = string.Empty;

    [JsonPropertyName("ErrorDescription")]
    public string? ErrorDescription { get; set; }

    [JsonPropertyName("Details")]
    public JsonElement? Details { get; set; }
}
