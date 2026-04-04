namespace finance_tracker_backend.Contracts.Responses;

public sealed class ProfileRecurringCashflowCalendarOccurrenceResponse
{
    public DateOnly Date { get; init; }
    public decimal Amount { get; init; }
    public string? MerchantName { get; init; }
    public string? Description { get; init; }
    public string? PfcPrimary { get; init; }
    public string? PfcDetailed { get; init; }
    public Guid RecurringCashflowId { get; init; }
    public string Frequency { get; init; } = string.Empty;
    public string Direction { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public RecurringCashflowLinkedBankAccountResponse? LinkedBankAccount { get; init; }
}
