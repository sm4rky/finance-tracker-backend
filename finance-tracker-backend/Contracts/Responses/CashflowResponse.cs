namespace finance_tracker_backend.Contracts.Responses;

public sealed class CashflowResponse
{
    public decimal TotalIncome { get; set; }
    public decimal? IncomeChangePercentFromPrevious { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal? ExpensesChangePercentFromPrevious { get; set; }
    public decimal? SavingsRate { get; set; }
    public decimal? SavingsRateChangePercentFromPrevious { get; set; }
}
