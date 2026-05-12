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

    Task<SyncPlaidTransactionsResponse> SyncLinkedBankForProfileAsync(
        Guid profileId,
        Guid linkedBankId,
        CancellationToken cancellationToken = default,
        bool bypassCooldown = true);

    Task SyncActiveLinkedBanksAsync(CancellationToken cancellationToken = default);
}