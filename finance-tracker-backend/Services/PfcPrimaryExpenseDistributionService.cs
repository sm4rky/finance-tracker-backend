using System.Security.Claims;
using finance_tracker_backend.Contracts.Requests;
using finance_tracker_backend.Contracts.Responses;
using finance_tracker_backend.Infrastructure;
using finance_tracker_backend.Repositories;

namespace finance_tracker_backend.Services;

public sealed class PfcPrimaryExpenseDistributionService(ITransactionRepository transactionRepository)
    : IPfcPrimaryExpenseDistributionService
{
    public async Task<PfcPrimaryExpenseDistributionResponse> GetAsync(
        ClaimsPrincipal user,
        TransactionAnalyticsQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var profileId = user.RequireProfileId();
        var filters = TransactionQueryFilterHelper.CreateForAnalyticsAggregation(request);

        var rows = await transactionRepository
            .SumExpensesByPfcPrimaryAsync(profileId, filters, cancellationToken)
            .ConfigureAwait(false);

        var slices = rows
            .Select(r => new PfcPrimaryExpenseSliceResponse
            {
                PfcPrimary = r.PfcPrimary,
                TotalExpenses = r.TotalExpenses
            })
            .ToList();

        return new PfcPrimaryExpenseDistributionResponse { Slices = slices };
    }
}
