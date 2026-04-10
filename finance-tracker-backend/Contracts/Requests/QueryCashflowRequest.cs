namespace finance_tracker_backend.Contracts.Requests;

public sealed class QueryCashflowRequest
{
    public List<Guid>? AccountIds { get; set; }
    public bool? IncludeUnlinkedTransactions { get; set; }
    public List<string>? PfcPrimaryList { get; set; }
    public List<string>? PaymentChannels { get; set; }
    public bool? Pending { get; set; }
    public string? DateFrom { get; set; }
    public string? DateTo { get; set; }
    public decimal? AmountMin { get; set; }
    public decimal? AmountMax { get; set; }
    public string? AmountFlow { get; set; }
}
