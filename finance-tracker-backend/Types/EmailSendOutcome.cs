namespace finance_tracker_backend.Types;

public sealed record EmailSendOutcome(bool Success, string? ProviderMessageId, string? ErrorMessage);
