using System.Text.Json.Serialization;

namespace finance_tracker_backend.Types;

internal sealed class ResendEmailIdResponse
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }
}
