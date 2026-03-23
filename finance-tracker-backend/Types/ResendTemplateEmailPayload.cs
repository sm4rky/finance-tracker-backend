using System.Text.Json.Serialization;

namespace finance_tracker_backend.Types;

internal sealed record ResendTemplateEmailPayload(
    [property: JsonPropertyName("from")] string From,
    [property: JsonPropertyName("to")] string[] To,
    [property: JsonPropertyName("template")]
    ResendTemplateReference Template);

internal sealed record ResendTemplateReference(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("variables")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    IReadOnlyDictionary<string, string>? Variables);