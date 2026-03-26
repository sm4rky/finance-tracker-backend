using finance_tracker_backend.Repositories;

namespace finance_tracker_backend.Jobs;

/// <summary>Hangfire recurring job: deletes expired rows from <c>plaid_link_sessions</c>. Scheduled in <c>Program.cs</c> after <c>WebApplication.Build</c>.</summary>
public sealed class ExpiredPlaidLinkSessionsCleanupJob(
    IServiceScopeFactory scopeFactory,
    ILogger<ExpiredPlaidLinkSessionsCleanupJob> logger)
{
    public async Task RunAsync()
    {
        using var scope = scopeFactory.CreateScope();
        var plaidLinkSessionRepository = scope.ServiceProvider.GetRequiredService<IPlaidLinkSessionRepository>();
        await plaidLinkSessionRepository.DeleteExpiredAsync().ConfigureAwait(false);
        logger.LogInformation("Plaid link session cleanup finished (expired rows removed).");
    }
}
