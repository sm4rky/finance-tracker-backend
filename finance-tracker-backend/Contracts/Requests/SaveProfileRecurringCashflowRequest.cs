namespace finance_tracker_backend.Contracts.Requests;

public sealed class SaveProfileRecurringCashflowRequest
{
    public Guid? LinkedBankAccountId { get; set; }
    public string Direction { get; set; } = string.Empty;
    public string? MerchantName { get; set; }
    public string? Description { get; set; }
    public string? PfcPrimary { get; set; }
    public string? PfcDetailed { get; set; }
    public string Frequency { get; set; } = string.Empty;
    public decimal? LastAmount { get; set; }
    public decimal ExpectedAmount { get; set; }
    public DateOnly? FirstDate { get; set; }
    public DateOnly? LastDate { get; set; }
    public DateOnly? PredictedNextDate { get; set; }
}
