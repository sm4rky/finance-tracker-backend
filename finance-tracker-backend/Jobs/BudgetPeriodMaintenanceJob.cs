using finance_tracker_backend.Services;

namespace finance_tracker_backend.Jobs;

public sealed class BudgetPeriodMaintenanceJob(
    IServiceScopeFactory scopeFactory,
    ILogger<BudgetPeriodMaintenanceJob> logger)
{
    public async Task RunAsync()
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var maintenanceService = scope.ServiceProvider.GetRequiredService<IBudgetPeriodMaintenanceService>();
            await maintenanceService.RunDailyMaintenanceAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Budget period maintenance job failed.");
            throw;
        }
    }
}
