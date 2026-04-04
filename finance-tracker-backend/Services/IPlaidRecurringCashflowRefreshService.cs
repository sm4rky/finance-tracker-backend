using System.Security.Claims;
using finance_tracker_backend.Contracts.Responses;

namespace finance_tracker_backend.Services;

public interface IPlaidRecurringCashflowRefreshService
{
    Task<SyncPlaidRecurringCashflowsResponse> SyncLinkedBankRecurringCashflowsAsync(
        ClaimsPrincipal user,
        Guid linkedBankId,
        CancellationToken cancellationToken = default);
}
