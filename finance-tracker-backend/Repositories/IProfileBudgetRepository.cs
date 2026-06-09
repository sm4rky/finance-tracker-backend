using finance_tracker_backend.Models;

namespace finance_tracker_backend.Repositories;

public interface IProfileBudgetRepository
{
    Task<ProfileBudget?> GetByIdAndProfileAsync(
        Guid id,
        Guid profileId,
        CancellationToken cancellationToken = default);

    Task<ProfileBudget?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProfileBudget>> ListByProfileIdAsync(
        Guid profileId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProfileBudget>> ListActiveRecurringAsync(
        CancellationToken cancellationToken = default);

    Task InsertAsync(ProfileBudget budget, CancellationToken cancellationToken = default);

    Task UpdateAsync(ProfileBudget budget, CancellationToken cancellationToken = default);

    Task DeactivateAsync(Guid id, DateTimeOffset updatedAt, CancellationToken cancellationToken = default);

    Task DeleteByIdAndProfileAsync(Guid id, Guid profileId, CancellationToken cancellationToken = default);
}
