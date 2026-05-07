using finance_tracker_backend.Models;

namespace finance_tracker_backend.Repositories;

public interface IProfileRepository
{
    Task<bool> ExistsAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<Profile?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<Profile?> GetByUsernameAsync(string normalizedUsername,
        CancellationToken cancellationToken = default);

    Task InsertAsync(Profile profile, CancellationToken cancellationToken = default);

    Task UpdateUsernameAsync(Guid profileId, string normalizedUsername,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Guid>> ListAllProfileIdsAsync(CancellationToken cancellationToken = default);
}
