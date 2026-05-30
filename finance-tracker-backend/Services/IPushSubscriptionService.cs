using System.Security.Claims;
using finance_tracker_backend.Contracts.Requests;
using finance_tracker_backend.Contracts.Responses;

namespace finance_tracker_backend.Services;

public interface IPushSubscriptionService
{
    Task<IReadOnlyList<PushSubscriptionResponse>> ListMyPushSubscriptionsAsync(
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PushSubscriptionResponse>> ListProfilePushSubscriptionsAsync(
        ClaimsPrincipal user,
        Guid profileId,
        CancellationToken cancellationToken = default);

    Task<PushSubscriptionResponse> UpsertMyPushSubscriptionAsync(
        ClaimsPrincipal user,
        UpsertPushSubscriptionRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteMyPushSubscriptionAsync(
        ClaimsPrincipal user,
        Guid id,
        CancellationToken cancellationToken = default);
}
