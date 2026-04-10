using System.Security.Claims;
using finance_tracker_backend.Contracts.Requests;
using finance_tracker_backend.Contracts.Responses;
using finance_tracker_backend.Infrastructure;
using finance_tracker_backend.Repositories;
using finance_tracker_backend.Types;

namespace finance_tracker_backend.Services;

public sealed class CashflowService(ITransactionRepository transactionRepository) : ICashflowService
{
    public async Task<CashflowResponse> GetAsync(
        ClaimsPrincipal user,
        TransactionAnalyticsQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var profileId = user.RequireProfileId();

        if (string.IsNullOrWhiteSpace(request.DateFrom) || string.IsNullOrWhiteSpace(request.DateTo))
            throw new ArgumentException("dateFrom and dateTo are required.");

        var filters = TransactionQueryFilterHelper.CreateForAnalyticsAggregation(request);
        if (filters.DateFromInclusive is not { } from || filters.DateToInclusive is not { } to)
            throw new ArgumentException("dateFrom and dateTo are required.");

        if (to < from)
            throw new ArgumentException("dateTo must be on or after dateFrom.");

        var (prevFrom, prevTo) = ComputePreviousInclusivePeriod(from, to);
        var previousFilters = filters with { DateFromInclusive = prevFrom, DateToInclusive = prevTo };

        var (incomeCurrent, expensesCurrent) = await transactionRepository
            .SumIncomeAndExpenseAsync(profileId, filters, cancellationToken)
            .ConfigureAwait(false);
        var (incomePrevious, expensesPrevious) = await transactionRepository
            .SumIncomeAndExpenseAsync(profileId, previousFilters, cancellationToken)
            .ConfigureAwait(false);

        var savingsCurrent = SavingsRatePercent(incomeCurrent, expensesCurrent);
        var savingsPrevious = SavingsRatePercent(incomePrevious, expensesPrevious);

        return new CashflowResponse
        {
            TotalIncome = incomeCurrent,
            IncomeChangePercentFromPrevious = PercentChange(incomeCurrent, incomePrevious),
            TotalExpenses = expensesCurrent,
            ExpensesChangePercentFromPrevious = PercentChange(expensesCurrent, expensesPrevious),
            SavingsRate = savingsCurrent,
            SavingsRateChangePercentFromPrevious = savingsCurrent is { } sc && savingsPrevious is { } sp
                ? PercentChange(sc, sp)
                : null
        };
    }

    private static (DateOnly PrevFrom, DateOnly PrevTo) ComputePreviousInclusivePeriod(DateOnly from, DateOnly to)
    {
        var spanDays = to.DayNumber - from.DayNumber + 1;
        var prevTo = from.AddDays(-1);
        var prevFrom = prevTo.AddDays(-(spanDays - 1));
        return (prevFrom, prevTo);
    }

    private static decimal? SavingsRatePercent(decimal totalIncome, decimal totalExpenses)
    {
        if (totalIncome <= 0m)
            return null;

        return (totalIncome - totalExpenses) / totalIncome * 100m;
    }

    private static decimal? PercentChange(decimal current, decimal previous)
    {
        if (previous == 0m)
            return null;

        return (current - previous) / previous * 100m;
    }
}
