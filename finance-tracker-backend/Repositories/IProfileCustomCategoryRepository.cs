using finance_tracker_backend.Models;

namespace finance_tracker_backend.Repositories;

public interface IProfileCustomCategoryRepository
{
    Task<IReadOnlyList<ProfileCustomCategory>> ListBySetIdAsync(
        Guid customCategorySetId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProfileCustomCategory>> ListBySetIdsAsync(
        IReadOnlyCollection<Guid> customCategorySetIds,
        CancellationToken cancellationToken = default);

    Task InsertAsync(ProfileCustomCategory category, CancellationToken cancellationToken = default);
    Task UpdateAsync(ProfileCustomCategory category, CancellationToken cancellationToken = default);

    Task DeleteMissingBySetIdAsync(
        Guid customCategorySetId,
        IReadOnlyCollection<Guid> keepCategoryIds,
        CancellationToken cancellationToken = default);
}
