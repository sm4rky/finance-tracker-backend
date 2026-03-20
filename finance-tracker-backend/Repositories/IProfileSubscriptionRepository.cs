using finance_tracker_backend.Models;

namespace finance_tracker_backend.Repositories;

public interface IProfileSubscriptionRepository
{
    Task<bool> ExistsForProfileAsync(Guid profileId, CancellationToken cancellationToken = default);

    Task<ProfileSubscription?> GetForProfileAsync(Guid profileId, CancellationToken cancellationToken = default);

    Task InsertAsync(ProfileSubscription subscription, CancellationToken cancellationToken = default);
}
