namespace finance_tracker_backend.Contracts.Responses;

public sealed class PfcPrimaryExpenseDistributionResponse
{
    public IReadOnlyList<PfcPrimaryExpenseSliceResponse> Slices { get; set; } = [];
}

public sealed class PfcPrimaryExpenseSliceResponse
{
    public string? PfcPrimary { get; set; }
    public decimal TotalExpenses { get; set; }
}
