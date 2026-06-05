using System.Security.Claims;
using finance_tracker_backend.Contracts.Requests;
using finance_tracker_backend.Contracts.Responses;
using finance_tracker_backend.Infrastructure;
using finance_tracker_backend.Repositories;

namespace finance_tracker_backend.Services;

public sealed class GroupedExpensesByAccountService(
    ITransactionRepository transactionRepository,
    CustomCategorySetHelper customCategorySetHelper)
    : IGroupedExpensesByAccountService
{
    public async Task<GroupedExpensesByAccountResponse> GetAsync(
        ClaimsPrincipal user,
        GroupedExpensesByAccountQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var profileId = user.RequireProfileId();

        if (string.IsNullOrWhiteSpace(request.DateFrom) || string.IsNullOrWhiteSpace(request.DateTo))
            throw new ArgumentException("dateFrom and dateTo are required.");

        var timeGranularity = AnalyticsTimeGranularityHelper.Parse(request.TimeGranularity);

        var customCategorySetData = request.CustomCategorySetId is { } customCategorySetId
            ? await customCategorySetHelper
                .LoadCustomCategoryByPfcPrimaryAsync(profileId, customCategorySetId, request.CustomCategoryIds, cancellationToken)
                .ConfigureAwait(false)
            : null;
        var customPfcPrimaryList = customCategorySetData is not null && request.CustomCategoryIds is { Count: > 0 }
            ? customCategorySetData.Keys
                .Select(pfcPrimary => pfcPrimary.PfcPrimaryCode)
                .Where(code => !string.IsNullOrWhiteSpace(code))
                .Distinct(StringComparer.Ordinal)
                .ToList()
            : null;

        if (customPfcPrimaryList is not null)
            request.PfcPrimaryList = customPfcPrimaryList;

        var query = TransactionsQueryHelper.CreateForAnalyticsAggregation(request);
        if (query.DateFromInclusive is null || query.DateToInclusive is null)
            throw new ArgumentException("dateFrom and dateTo are required.");

        if (query.DateToInclusive < query.DateFromInclusive)
            throw new ArgumentException("dateTo must be on or after dateFrom.");

        var rows = await transactionRepository
            .GetGroupedExpensesByAccountSeriesAsync(profileId, query, timeGranularity, cancellationToken)
            .ConfigureAwait(false);

        var accountOrder = SharedAccountOrder(rows);

        var grouped = rows
            .GroupBy(r => r.PeriodStartDate)
            .OrderBy(g => g.Key)
            .ToList();

        var periodStartDates = grouped.Select(g => g.Key).ToList();
        var monthBucketsSpanMultipleYears =
            AnalyticsTimeGranularityHelper.MonthBucketsSpanMultipleYears(periodStartDates);
        var dateRangeSpansMultipleYears = AnalyticsTimeGranularityHelper.DateRangeSpansMultipleYears(
            query.DateFromInclusive!.Value,
            query.DateToInclusive!.Value);

        var buckets = grouped
            .Select(g => new GroupedExpensesByAccountBucket
            {
                Period = AnalyticsTimeGranularityHelper.FormatPeriodLabel(
                    g.Key,
                    timeGranularity,
                    monthBucketsSpanMultipleYears,
                    dateRangeSpansMultipleYears),
                Bars = g.Select(x => new GroupedExpensesByAccountBar
                {
                    LinkedBankAccountId = x.LinkedBankAccountId,
                    OfficialName = x.OfficialName,
                    Amount = x.ExpenseTotal
                })
                    .OrderBy(x => accountOrder.IndexOf(x.LinkedBankAccountId))
                    .ToList()
            })
            .ToList();

        return new GroupedExpensesByAccountResponse
        {
            TimeGranularity = timeGranularity,
            Buckets = buckets
        };
    }

    private static List<Guid?> SharedAccountOrder(
        IEnumerable<(DateOnly PeriodStartDate, Guid? LinkedBankAccountId, string? OfficialName, decimal ExpenseTotal)>
            rows) =>
        rows
            .GroupBy(r => r.LinkedBankAccountId)
            .Select(g => (LinkedBankAccountId: g.Key, Total: g.Sum(x => x.ExpenseTotal)))
            .OrderByDescending(x => x.Total)
            .ThenBy(x => x.LinkedBankAccountId, NullableGuidComparer.Instance)
            .Select(x => x.LinkedBankAccountId)
            .ToList();

    private sealed class NullableGuidComparer : IComparer<Guid?>
    {
        public static readonly NullableGuidComparer Instance = new();

        public int Compare(Guid? x, Guid? y)
        {
            if (x is null && y is null)
                return 0;
            if (x is null)
                return 1;
            if (y is null)
                return -1;
            return x.Value.CompareTo(y.Value);
        }
    }
}
