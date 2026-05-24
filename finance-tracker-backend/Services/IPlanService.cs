using System.Security.Claims;
using finance_tracker_backend.Contracts.Responses;

namespace finance_tracker_backend.Services;

public interface IPlanService
{
    Task<IReadOnlyList<SubscriptionPlanResponse>> ListSubscriptionPlansAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SubscriptionPaymentResponse>> ListAllSubscriptionPaymentsAsync(
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SubscriptionPaymentResponse>> ListMySubscriptionPaymentsAsync(
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SubscriptionPaymentResponse>> ListProfileSubscriptionPaymentsAsync(
        ClaimsPrincipal user,
        Guid profileId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SubscriptionPaymentHistoryResponse>> ListAllSubscriptionPaymentHistoryAsync(
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SubscriptionPaymentHistoryResponse>> ListMySubscriptionPaymentHistoryAsync(
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SubscriptionPaymentHistoryResponse>> ListProfileSubscriptionPaymentHistoryAsync(
        ClaimsPrincipal user,
        Guid profileId,
        CancellationToken cancellationToken = default);
}
