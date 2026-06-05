using System.Security.Claims;
using finance_tracker_backend.Contracts.Requests;
using finance_tracker_backend.Contracts.Responses;
using finance_tracker_backend.Infrastructure;
using finance_tracker_backend.Repositories;

namespace finance_tracker_backend.Services;

public sealed class StackedExpensesByCategoryService(
    ITransactionRepository transactionRepository,
    CustomCategorySetHelper customCategorySetHelper)
    : IStackedExpensesByCategoryService
{
    public async Task<StackedExpensesByCategoryResponse> GetAsync(
        ClaimsPrincipal user,
        StackedExpensesByCategoryQueryRequest request,
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
            .GetStackedExpensesByPfcPrimarySeriesAsync(profileId, query, timeGranularity, cancellationToken)
            .ConfigureAwait(false);

        var categoryRows = rows
            .Select(row => new CategorySeriesRow(
                row.PeriodStartDate,
                row.PfcPrimary,
                customCategorySetData != null
                    ? CustomCategorySetHelper.GetCustomCategoryResponseByPfcPrimaryCodeAndPfcVersion(
                        customCategorySetData,
                        row.PfcPrimary,
                        CustomCategorySetHelper.PfcVersion)
                    : null,
                row.ExpenseTotal))
            .GroupBy(r => new
            {
                r.PeriodStartDate,
                CategoryId = r.CustomCategory?.Id,
                PfcPrimary = r.CustomCategory is null ? r.PfcPrimary : null
            })
            .Select(g =>
            {
                var first = g.First();
                return new CategorySeriesRow(
                    first.PeriodStartDate,
                    first.CustomCategory is null ? first.PfcPrimary : null,
                    first.CustomCategory,
                    g.Sum(x => x.ExpenseTotal));
            })
            .ToList();

        var stackOrder = SharedStackOrder(categoryRows);

        var grouped = categoryRows
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
            .Select(g => new StackedExpensesByCategoryBucket
            {
                Period = AnalyticsTimeGranularityHelper.FormatPeriodLabel(
                    g.Key,
                    timeGranularity,
                    monthBucketsSpanMultipleYears,
                    dateRangeSpansMultipleYears),
                Stacks = g.Select(x => new StackedExpensesByCategoryAmount
                {
                    PfcPrimary = x.PfcPrimary,
                    CustomCategory = x.CustomCategory,
                    Amount = x.ExpenseTotal
                })
                    .OrderBy(x => stackOrder.IndexOf(StackKey(x.PfcPrimary, x.CustomCategory)))
                    .ToList()
            })
            .ToList();

        return new StackedExpensesByCategoryResponse
        {
            TimeGranularity = timeGranularity,
            Buckets = buckets
        };
    }

    private static List<string> SharedStackOrder(IEnumerable<CategorySeriesRow> rows) =>
        rows
            .GroupBy(r => StackKey(r.PfcPrimary, r.CustomCategory))
            .Select(g => (Key: g.Key, Total: g.Sum(x => x.ExpenseTotal)))
            .OrderByDescending(x => x.Total)
            .ThenBy(x => x.Key, StringComparer.Ordinal)
            .Select(x => x.Key)
            .ToList();

    private static string StackKey(string? pfcPrimary, ProfileCustomCategoryResponse? customCategory) =>
        customCategory is null ? $"pfc:{pfcPrimary}" : $"category:{customCategory.Id}";

    private sealed record CategorySeriesRow(
        DateOnly PeriodStartDate,
        string? PfcPrimary,
        ProfileCustomCategoryResponse? CustomCategory,
        decimal ExpenseTotal);
}
