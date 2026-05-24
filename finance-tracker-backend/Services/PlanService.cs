using System.Security.Claims;
using finance_tracker_backend.Contracts.Responses;
using finance_tracker_backend.Infrastructure;
using finance_tracker_backend.Models;
using finance_tracker_backend.Repositories;

namespace finance_tracker_backend.Services;

public sealed class PlanService(
    IPlanRepository planRepository,
    IProfileService profileService) : IPlanService
{
    public async Task<IReadOnlyList<SubscriptionPlanResponse>> ListSubscriptionPlansAsync(
        CancellationToken cancellationToken = default)
    {
        var plans = await planRepository.ListSubscriptionPlansAsync(cancellationToken).ConfigureAwait(false);
        return plans
            .OrderBy(x => x.MonthlyPrice ?? decimal.MaxValue)
            .ThenBy(x => x.Id, StringComparer.Ordinal)
            .Select(ToResponse)
            .ToList();
    }

    public async Task<IReadOnlyList<SubscriptionPaymentResponse>> ListAllSubscriptionPaymentsAsync(
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        await RequireAdminAsync(user, cancellationToken).ConfigureAwait(false);

        var payments = await planRepository.ListAllSubscriptionPaymentsAsync(cancellationToken).ConfigureAwait(false);
        return MapPayments(payments);
    }

    public async Task<IReadOnlyList<SubscriptionPaymentResponse>> ListMySubscriptionPaymentsAsync(
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        var profileId = user.RequireProfileId();
        var payments = await planRepository
            .ListProfileSubscriptionPaymentsAsync(profileId, cancellationToken)
            .ConfigureAwait(false);
        return MapPayments(payments);
    }

    public async Task<IReadOnlyList<SubscriptionPaymentResponse>> ListProfileSubscriptionPaymentsAsync(
        ClaimsPrincipal user,
        Guid profileId,
        CancellationToken cancellationToken = default)
    {
        await RequireAdminAsync(user, cancellationToken).ConfigureAwait(false);

        var payments = await planRepository
            .ListProfileSubscriptionPaymentsAsync(profileId, cancellationToken)
            .ConfigureAwait(false);
        return MapPayments(payments);
    }

    public async Task<IReadOnlyList<SubscriptionPaymentHistoryResponse>> ListAllSubscriptionPaymentHistoryAsync(
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        await RequireAdminAsync(user, cancellationToken).ConfigureAwait(false);

        var history = await planRepository.ListSubscriptionHistoryAsync(cancellationToken).ConfigureAwait(false);
        return MapHistory(history);
    }

    public async Task<IReadOnlyList<SubscriptionPaymentHistoryResponse>> ListMySubscriptionPaymentHistoryAsync(
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        var profileId = user.RequireProfileId();
        var history = await planRepository
            .ListSubscriptionHistoryForProfileAsync(profileId, cancellationToken)
            .ConfigureAwait(false);
        return MapHistory(history);
    }

    public async Task<IReadOnlyList<SubscriptionPaymentHistoryResponse>> ListProfileSubscriptionPaymentHistoryAsync(
        ClaimsPrincipal user,
        Guid profileId,
        CancellationToken cancellationToken = default)
    {
        await RequireAdminAsync(user, cancellationToken).ConfigureAwait(false);

        var history = await planRepository
            .ListSubscriptionHistoryForProfileAsync(profileId, cancellationToken)
            .ConfigureAwait(false);
        return MapHistory(history);
    }

    private async Task RequireAdminAsync(ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        var profileId = user.RequireProfileId();
        var profile = await profileService.GetByIdAsync(profileId, cancellationToken).ConfigureAwait(false);
        if (!string.Equals(profile?.Role, "admin", StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("Admin access is required.");
    }

    private static IReadOnlyList<SubscriptionPaymentResponse> MapPayments(
        IEnumerable<SubscriptionPayment> payments) =>
        payments
            .OrderByDescending(x => x.ChargedAt)
            .ThenByDescending(x => x.CreatedAt)
            .ThenBy(x => x.Id)
            .Select(ToResponse)
            .ToList();

    private static IReadOnlyList<SubscriptionPaymentHistoryResponse> MapHistory(
        IEnumerable<SubscriptionHistory> history) =>
        history
            .OrderByDescending(x => x.EffectiveAt)
            .ThenByDescending(x => x.CreatedAt)
            .ThenBy(x => x.Id)
            .Select(ToResponse)
            .ToList();

    private static SubscriptionPlanResponse ToResponse(SubscriptionPlan plan) => new()
    {
        Id = plan.Id,
        Name = plan.Name,
        MaxAccounts = plan.MaxAccounts,
        HistoryMonths = plan.HistoryMonths,
        HasAi = plan.HasAi,
        MonthlyPrice = plan.MonthlyPrice
    };

    private static SubscriptionPaymentResponse ToResponse(SubscriptionPayment payment) => new()
    {
        Id = payment.Id,
        ProfileId = payment.ProfileId,
        Amount = payment.Amount,
        ChargedAt = payment.ChargedAt,
        PlanId = payment.PlanId,
        PaymentType = payment.PaymentType,
        Reference = payment.Reference,
        CreatedAt = payment.CreatedAt
    };

    private static SubscriptionPaymentHistoryResponse ToResponse(SubscriptionHistory history) => new()
    {
        Id = history.Id,
        ProfileId = history.ProfileId,
        EventType = history.EventType,
        FromPlanId = history.FromPlanId,
        ToPlanId = history.ToPlanId,
        CreatedAt = history.CreatedAt,
        EffectiveAt = history.EffectiveAt
    };
}
