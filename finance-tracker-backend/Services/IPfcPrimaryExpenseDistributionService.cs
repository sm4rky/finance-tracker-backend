using System.Security.Claims;
using finance_tracker_backend.Contracts.Requests;
using finance_tracker_backend.Contracts.Responses;

namespace finance_tracker_backend.Services;

public interface IPfcPrimaryExpenseDistributionService
{
    Task<PfcPrimaryExpenseDistributionResponse> GetAsync(
        ClaimsPrincipal user,
        TransactionAnalyticsQueryRequest request,
        CancellationToken cancellationToken = default);
}
