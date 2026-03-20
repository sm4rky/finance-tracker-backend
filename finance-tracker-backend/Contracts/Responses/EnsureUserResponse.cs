using System.Text.Json.Serialization;

namespace finance_tracker_backend.Contracts.Responses;

public sealed class EnsureUserResponse
{
    public required string Email { get; init; }

    public required string FullName { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? AvatarUrl { get; init; }

    public required string Role { get; init; }

    public required string Plan { get; init; }
}
