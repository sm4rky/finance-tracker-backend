namespace finance_tracker_backend.Types;

public sealed record PushSendOutcome(bool Success, string? ErrorMessage, bool ShouldDeleteSubscription);
