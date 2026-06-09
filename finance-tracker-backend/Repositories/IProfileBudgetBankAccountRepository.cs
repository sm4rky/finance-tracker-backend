using finance_tracker_backend.Models;

namespace finance_tracker_backend.Repositories;

public interface IProfileBudgetBankAccountRepository
{
    Task<IReadOnlyList<ProfileBudgetBankAccount>> ListByBudgetIdAsync(
        Guid budgetId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProfileBudgetBankAccount>> ListByBudgetIdsAsync(
        IReadOnlyCollection<Guid> budgetIds,
        CancellationToken cancellationToken = default);

    Task ReplaceForBudgetAsync(
        Guid budgetId,
        IReadOnlyList<ProfileBudgetBankAccount> accounts,
        CancellationToken cancellationToken = default);
}
