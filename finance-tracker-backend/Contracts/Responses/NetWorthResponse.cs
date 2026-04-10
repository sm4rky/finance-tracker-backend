namespace finance_tracker_backend.Contracts.Responses;

public sealed class NetWorthResponse
{
    public decimal TotalAssets { get; set; }
    public decimal TotalLiabilities { get; set; }
    public decimal NetWorth { get; set; }
}
