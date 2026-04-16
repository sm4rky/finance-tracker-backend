using System.Security.Claims;
using finance_tracker_backend.Contracts.Requests;
using finance_tracker_backend.Contracts.Responses;
using finance_tracker_backend.Infrastructure;
using finance_tracker_backend.Repositories;

namespace finance_tracker_backend.Services;

public sealed class StackedExpensesByPfcPrimaryService(ITransactionRepository transactionRepository)
    : IStackedExpensesByPfcPrimaryService
{
    public async Task<StackedExpensesByPfcPrimaryResponse> GetAsync(
        ClaimsPrincipal user,
        StackedExpensesByPfcPrimaryQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var profileId = user.RequireProfileId();

        if (string.IsNullOrWhiteSpace(request.DateFrom) || string.IsNullOrWhiteSpace(request.DateTo))
            throw new ArgumentException("dateFrom and dateTo are required.");

        var timeGranularity = AnalyticsTimeGranularityHelper.Parse(request.TimeGranularity);

        var filters = TransactionQueryFilterHelper.CreateForAnalyticsAggregation(request);
        if (filters.DateFromInclusive is null || filters.DateToInclusive is null)
            throw new ArgumentException("dateFrom and dateTo are required.");

        if (filters.DateToInclusive < filters.DateFromInclusive)
            throw new ArgumentException("dateTo must be on or after dateFrom.");

        var rows = await transactionRepository
            .GetStackedExpensesByPfcPrimarySeriesAsync(profileId, filters, timeGranularity, cancellationToken)
            .ConfigureAwait(false);

        var pfcPrimaryOrder = SharedPfcPrimaryOrder(rows);

        var grouped = rows
            .GroupBy(r => r.PeriodStartDate)
            .OrderBy(g => g.Key)
            .ToList();

        var periodStartDates = grouped.Select(g => g.Key).ToList();
        var monthBucketsSpanMultipleYears =
            AnalyticsTimeGranularityHelper.MonthBucketsSpanMultipleYears(periodStartDates);
        var dateRangeSpansMultipleYears = AnalyticsTimeGranularityHelper.DateRangeSpansMultipleYears(
            filters.DateFromInclusive!.Value,
            filters.DateToInclusive!.Value);

        var buckets = grouped
            .Select(g => new StackedExpensesByPfcPrimaryBucket
            {
                Period = AnalyticsTimeGranularityHelper.FormatPeriodLabel(
                    g.Key,
                    timeGranularity,
                    monthBucketsSpanMultipleYears,
                    dateRangeSpansMultipleYears),
                Stacks = g.Select(x => new StackedExpensesByPfcPrimaryAmount
                {
                    PfcPrimary = x.PfcPrimary,
                    Amount = x.ExpenseTotal
                })
                    .OrderBy(x => pfcPrimaryOrder.IndexOf(x.PfcPrimary))
                    .ToList()
            })
            .ToList();

        return new StackedExpensesByPfcPrimaryResponse
        {
            TimeGranularity = timeGranularity,
            Buckets = buckets
        };
    }

    private static List<string?> SharedPfcPrimaryOrder(
        IEnumerable<(DateOnly PeriodStartDate, string? PfcPrimary, decimal ExpenseTotal)> rows) =>
        rows
            .GroupBy(r => r.PfcPrimary)
            .Select(g => (PfcPrimary: g.Key, Total: g.Sum(x => x.ExpenseTotal)))
            .OrderByDescending(x => x.Total)
            .ThenBy(x => x.PfcPrimary, StringComparer.Ordinal)
            .Select(x => x.PfcPrimary)
            .ToList();
}
