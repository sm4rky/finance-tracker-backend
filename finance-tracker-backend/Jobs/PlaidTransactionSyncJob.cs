using finance_tracker_backend.Services;

namespace finance_tracker_backend.Jobs;

public sealed class PlaidTransactionSyncJob(
    IServiceScopeFactory scopeFactory,
    ILogger<PlaidTransactionSyncJob> logger)
{
    public async Task RunAsync()
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var plaidTransactionSyncService = scope.ServiceProvider.GetRequiredService<IPlaidTransactionSyncService>();
            await plaidTransactionSyncService.SyncActiveLinkedBanksAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Plaid transaction sync job failed.");
            throw;
        }
    }
}