using finance_tracker_backend.Services;

namespace finance_tracker_backend.Jobs;

/// <summary>Upserts monthly net worth per profile.</summary>
public sealed class MonthlyNetWorthJob(
    IServiceScopeFactory scopeFactory,
    ILogger<MonthlyNetWorthJob> logger)
{
    public async Task RunAsync()
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var netWorthService = scope.ServiceProvider.GetRequiredService<INetWorthService>();
            await netWorthService.UpsertProfileMonthlyNetWorthForProfilesAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Monthly net worth job failed.");
            throw;
        }
    }
}
