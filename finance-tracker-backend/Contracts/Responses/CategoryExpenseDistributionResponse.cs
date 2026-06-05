namespace finance_tracker_backend.Contracts.Responses;

public sealed class CategoryExpenseDistributionResponse
{
    public IReadOnlyList<CategoryExpenseSliceResponse> Slices { get; set; } = [];
}

public sealed class CategoryExpenseSliceResponse
{
    public string? PfcPrimary { get; set; }
    public ProfileCustomCategoryResponse? CustomCategory { get; set; }
    public decimal TotalExpenses { get; set; }
}
