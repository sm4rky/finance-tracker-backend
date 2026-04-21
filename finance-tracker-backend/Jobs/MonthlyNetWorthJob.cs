using finance_tracker_backend.Repositories;
using finance_tracker_backend.Services;

namespace finance_tracker_backend.Jobs;

/// <summary>Upserts monthly net worth per profile. Cron + <see cref="TimeZoneInfo.Utc"/> match <c>ExpiredPlaidLinkSessionsCleanupJob</c>.</summary>
public sealed class MonthlyNetWorthJob(
    IServiceScopeFactory scopeFactory,
    ILogger<MonthlyNetWorthJob> logger)
{
    public async Task RunAsync()
    {
        using var scope = scopeFactory.CreateScope();
        var profileRepository = scope.ServiceProvider.GetRequiredService<IProfileRepository>();
        var netWorthService = scope.ServiceProvider.GetRequiredService<INetWorthService>();
        var netWorthRepository = scope.ServiceProvider.GetRequiredService<IProfileMonthlyNetWorthRepository>();

        var profileIds = await profileRepository.ListAllProfileIdsAsync().ConfigureAwait(false);
        var periodStart = new DateOnly(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var createdAt = DateTimeOffset.UtcNow;

        var ok = 0;
        var failed = 0;

        foreach (var profileId in profileIds)
        {
            try
            {
                var nw = await netWorthService.GetNetWorthForProfileAsync(profileId).ConfigureAwait(false);
                await netWorthRepository
                    .UpsertAsync(
                        profileId,
                        periodStart,
                        nw.TotalAssets,
                        nw.TotalLiabilities,
                        nw.NetWorth,
                        createdAt)
                    .ConfigureAwait(false);
                ok++;
            }
            catch (Exception ex)
            {
                failed++;
                logger.LogError(ex, "Monthly net worth job failed for profile {ProfileId}.", profileId);
            }
        }

        logger.LogInformation(
            "Monthly net worth job finished for {Period}: {Ok} ok, {Failed} failed, {Total} profiles.",
            periodStart,
            ok,
            failed,
            profileIds.Count);
    }
}
