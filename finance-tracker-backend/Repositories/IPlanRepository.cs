using finance_tracker_backend.Models;

namespace finance_tracker_backend.Repositories;

public interface IPlanRepository
{
    Task<IReadOnlyList<SubscriptionPlan>> ListSubscriptionPlansAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SubscriptionPayment>> ListAllSubscriptionPaymentsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SubscriptionPayment>> ListProfileSubscriptionPaymentsAsync(
        Guid profileId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SubscriptionHistory>> ListSubscriptionHistoryAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SubscriptionHistory>> ListSubscriptionHistoryForProfileAsync(
        Guid profileId,
        CancellationToken cancellationToken = default);
}
