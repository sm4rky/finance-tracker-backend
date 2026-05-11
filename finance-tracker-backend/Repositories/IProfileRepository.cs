using finance_tracker_backend.Models;

namespace finance_tracker_backend.Repositories;

public interface IProfileRepository
{
    Task<bool> ExistsAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<Profile?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<Profile?> GetByUsernameAsync(string username,
        CancellationToken cancellationToken = default);

    Task InsertAsync(Profile profile, CancellationToken cancellationToken = default);

    Task UpdateUsernameAsync(Guid profileId, string username,
        CancellationToken cancellationToken = default);

    Task UpdatePasswordLoginEnabledAsync(Guid profileId, bool enabled,
        CancellationToken cancellationToken = default);

    Task UpdateAvatarUrlAsync(Guid profileId, string? avatarUrl,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Guid>> ListAllProfileIdsAsync(CancellationToken cancellationToken = default);
}
