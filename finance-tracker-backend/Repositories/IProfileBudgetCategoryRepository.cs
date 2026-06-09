using finance_tracker_backend.Models;

namespace finance_tracker_backend.Repositories;

public interface IProfileBudgetCategoryRepository
{
    Task<IReadOnlyList<ProfileBudgetCategory>> ListByBudgetIdAsync(
        Guid budgetId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProfileBudgetCategory>> ListByBudgetIdsAsync(
        IReadOnlyCollection<Guid> budgetIds,
        CancellationToken cancellationToken = default);

    Task ReplaceForBudgetAsync(
        Guid budgetId,
        IReadOnlyList<ProfileBudgetCategory> categories,
        CancellationToken cancellationToken = default);
}
