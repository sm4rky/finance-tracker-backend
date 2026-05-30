using finance_tracker_backend.Models;

namespace finance_tracker_backend.Repositories;

public interface IPushSubscriptionRepository
{
    Task<IReadOnlyList<PushSubscription>> ListByProfileIdAsync(
        Guid profileId,
        CancellationToken cancellationToken = default);

    Task<PushSubscription?> GetByIdForProfileAsync(
        Guid profileId,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<PushSubscription> UpsertByEndpointAsync(
        PushSubscription subscription,
        CancellationToken cancellationToken = default);

    Task<int> DeleteForProfileAsync(
        Guid profileId,
        Guid id,
        CancellationToken cancellationToken = default);
}
