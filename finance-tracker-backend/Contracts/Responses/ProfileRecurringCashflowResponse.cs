namespace finance_tracker_backend.Contracts.Responses;

public sealed class ProfileRecurringCashflowResponse
{
    public Guid Id { get; init; }
    public string? MerchantName { get; init; }
    public string? Description { get; init; }
    public string? PfcPrimary { get; init; }
    public string? PfcDetailed { get; init; }
    public string Direction { get; init; } = string.Empty;
    public string Frequency { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public decimal? LastAmount { get; init; }
    public decimal ExpectedAmount { get; init; }
    public bool ExpectedAmountUserSet { get; init; }
    public DateOnly? FirstDate { get; init; }
    public DateOnly? LastDate { get; init; }
    public DateOnly? PredictedNextDate { get; init; }
    public string? PlaidStreamId { get; init; }
    public RecurringCashflowLinkedBankAccountResponse? LinkedBankAccount { get; init; }
}
