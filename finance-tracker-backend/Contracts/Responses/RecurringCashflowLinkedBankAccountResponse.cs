namespace finance_tracker_backend.Contracts.Responses;

public sealed class RecurringCashflowLinkedBankAccountResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Mask { get; init; }
    public string? Type { get; init; }
    public string? Subtype { get; init; }
}
