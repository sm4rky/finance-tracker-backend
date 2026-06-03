using finance_tracker_backend.Models;

namespace finance_tracker_backend.Repositories;

public interface IProfileCustomCategoryPfcPrimaryRepository
{
    Task<IReadOnlyList<ProfileCustomCategoryPfcPrimary>> ListByCategoryIdAsync(
        Guid customCategoryId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProfileCustomCategoryPfcPrimary>> ListByCategoryIdsAsync(
        IReadOnlyCollection<Guid> customCategoryIds,
        CancellationToken cancellationToken = default);

    Task ReplaceForCategoryAsync(
        Guid customCategoryId,
        IReadOnlyList<ProfileCustomCategoryPfcPrimary> mappings,
        CancellationToken cancellationToken = default);
}
