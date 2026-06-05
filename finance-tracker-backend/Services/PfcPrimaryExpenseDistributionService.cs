using System.Security.Claims;
using finance_tracker_backend.Contracts.Requests;
using finance_tracker_backend.Contracts.Responses;
using finance_tracker_backend.Infrastructure;
using finance_tracker_backend.Repositories;

namespace finance_tracker_backend.Services;

public sealed class CategoryExpenseDistributionService(
    ITransactionRepository transactionRepository,
    CustomCategorySetHelper customCategorySetHelper)
    : ICategoryExpenseDistributionService
{
    public async Task<CategoryExpenseDistributionResponse> GetAsync(
        ClaimsPrincipal user,
        TransactionAnalyticsQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var profileId = user.RequireProfileId();
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

        var rows = await transactionRepository
            .SumExpensesByPfcPrimaryAsync(profileId, query, cancellationToken)
            .ConfigureAwait(false);

        var slices = rows
            .Select(r => new
            {
                r.PfcPrimary,
                CustomCategory = customCategorySetData is { } customCategorySet
                    ? CustomCategorySetHelper.GetCustomCategoryResponseByPfcPrimaryCodeAndPfcVersion(
                        customCategorySet,
                        r.PfcPrimary,
                        CustomCategorySetHelper.PfcVersion)
                    : null,
                r.TotalExpenses
            })
            .GroupBy(x => new
            {
                CategoryId = x.CustomCategory?.Id,
                PfcPrimary = x.CustomCategory is null ? x.PfcPrimary : null
            })
            .Select(g =>
            {
                var first = g.First();
                return new CategoryExpenseSliceResponse
                {
                    PfcPrimary = first.CustomCategory is null ? first.PfcPrimary : null,
                    CustomCategory = first.CustomCategory,
                    TotalExpenses = g.Sum(x => x.TotalExpenses)
                };
            })
            .OrderByDescending(s => s.TotalExpenses)
            .ThenBy(s => s.CustomCategory?.Name)
            .ThenBy(s => s.PfcPrimary)
            .ToList();

        return new CategoryExpenseDistributionResponse { Slices = slices };
    }
}
