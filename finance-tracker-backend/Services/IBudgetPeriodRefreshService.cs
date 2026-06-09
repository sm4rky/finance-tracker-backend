namespace finance_tracker_backend.Services;

public interface IBudgetPeriodRefreshService
{
    Task RefreshForProfileDatesAsync(
        Guid profileId,
        IReadOnlyCollection<DateOnly> dates,
        CancellationToken cancellationToken = default);

    Task RefreshPeriodAsync(
        Guid periodId,
        CancellationToken cancellationToken = default);
}
