using finance_tracker_backend.Models;

namespace finance_tracker_backend.Repositories;

public interface IProfileCustomCategorySetRepository
{
    Task<ProfileCustomCategorySet?> GetByIdAndProfileAsync(
        Guid id,
        Guid profileId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProfileCustomCategorySet>> ListByProfileIdAsync(
        Guid profileId,
        CancellationToken cancellationToken = default);

    Task InsertAsync(ProfileCustomCategorySet set, CancellationToken cancellationToken = default);
    Task UpdateAsync(ProfileCustomCategorySet set, CancellationToken cancellationToken = default);
    Task DeleteByIdAndProfileAsync(Guid id, Guid profileId, CancellationToken cancellationToken = default);
}
