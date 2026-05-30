using finance_tracker_backend.Models;

namespace finance_tracker_backend.Repositories;

public interface IProfileNotificationPreferenceRepository
{
    Task<ProfileNotificationPreference?> GetByProfileIdAsync(
        Guid profileId,
        CancellationToken cancellationToken = default);

    Task InsertDefaultAsync(Guid profileId, CancellationToken cancellationToken = default);

    Task UpdateAsync(ProfileNotificationPreference row, CancellationToken cancellationToken = default);
}
