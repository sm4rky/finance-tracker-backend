namespace finance_tracker_backend.Contracts.Responses;

public sealed class LinkedBankSummaryResponse
{
    public Guid Id { get; set; }
    public string PlaidItemId { get; set; } = string.Empty;
    public string? InstitutionId { get; set; }
    public string? InstitutionName { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset? DisconnectedAt { get; set; }
    public DateTimeOffset? TokenRemovedAt { get; set; }
    public DateTimeOffset? LastSyncedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public bool HasPendingUpdateAccountDecisions { get; set; }
    public IReadOnlyList<LinkedBankAccountResponse> Accounts { get; set; } = [];
}
