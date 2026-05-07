namespace finance_tracker_backend.Contracts.Requests;

public sealed class SetUsernameRequest
{
    public required string Username { get; init; }
}
