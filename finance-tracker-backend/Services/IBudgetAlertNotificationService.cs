using finance_tracker_backend.Models;

namespace finance_tracker_backend.Services;

public interface IBudgetAlertNotificationService
{
    Task SendBudgetAlertAsync(
        ProfileBudget budget,
        ProfileBudgetPeriod period,
        decimal spentAmount,
        CancellationToken cancellationToken = default);
}