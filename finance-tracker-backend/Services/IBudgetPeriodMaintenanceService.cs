using finance_tracker_backend.Models;

namespace finance_tracker_backend.Services;

public interface IBudgetPeriodMaintenanceService
{
    Task EnsureInitialPeriodsAsync(
        ProfileBudget budget,
        CancellationToken cancellationToken = default);

    Task RunDailyMaintenanceAsync(CancellationToken cancellationToken = default);
}
