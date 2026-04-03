namespace finance_tracker_backend.Contracts.Requests;

public sealed class SaveTransactionRequest
{
    public Guid? LinkedBankAccountId { get; set; }
    public decimal Amount { get; set; }
    public string? AmountFlow { get; set; }
    public string? Date { get; set; }
    public string? MerchantName { get; set; }
    public bool Pending { get; set; }
    public string? PaymentChannel { get; set; }
    public string? PfcPrimary { get; set; }
    public string? PfcDetailed { get; set; }
    public string? Website { get; set; }
    public string? Status { get; set; }
    public bool ClearLogo { get; set; }
}
