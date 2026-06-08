using finance_tracker_backend.Models;

namespace finance_tracker_backend.Repositories;

public interface IProfileBudgetPeriodRepository
{
    Task<ProfileBudgetPeriod?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ProfileBudgetPeriod?> GetByBudgetAndRangeAsync(
        Guid budgetId,
        DateOnly periodStartDate,
        DateOnly periodEndDate,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProfileBudgetPeriod>> ListByBudgetIdAsync(
        Guid budgetId,
        CancellationToken cancellationToken = default);

    Task<ProfileBudgetPeriod?> GetCurrentPeriodAsync(
        Guid budgetId,
        DateOnly today,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProfileBudgetPeriod>> ListActiveBudgetPeriodsContainingAnyDateAsync(
        Guid profileId,
        IReadOnlyCollection<DateOnly> dates,
        CancellationToken cancellationToken = default);

    Task InsertAsync(ProfileBudgetPeriod period, CancellationToken cancellationToken = default);

    Task UpdateSpentAmountAsync(
        Guid periodId,
        decimal spentAmount,
        DateTimeOffset updatedAt,
        CancellationToken cancellationToken = default);

    Task UpdateAmountLimitAsync(
        Guid periodId,
        decimal amountLimit,
        DateTimeOffset updatedAt,
        CancellationToken cancellationToken = default);
}
