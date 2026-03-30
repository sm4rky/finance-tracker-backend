using System.Security.Claims;
using finance_tracker_backend.Contracts.Responses;

namespace finance_tracker_backend.Services;

public interface IPlaidTransactionSyncService
{
    Task<SyncPlaidTransactionsResponse> SyncLinkedBankAsync(
        ClaimsPrincipal user,
        Guid linkedBankId,
        CancellationToken cancellationToken = default,
        bool bypassCooldown = false);
}
