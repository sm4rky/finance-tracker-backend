using System.Security.Claims;
using finance_tracker_backend.Contracts.Requests;
using finance_tracker_backend.Contracts.Responses;

namespace finance_tracker_backend.Services;

public interface IProfileBudgetService
{
    Task<IReadOnlyList<ProfileBudgetResponse>> ListAsync(
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default);

    Task<ProfileBudgetResponse> GetByIdAsync(
        ClaimsPrincipal user,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProfileBudgetPeriodResponse>> ListPeriodsAsync(
        ClaimsPrincipal user,
        Guid budgetId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProfileBudgetPeriodResponse>> ListOngoingPeriodsAsync(
        ClaimsPrincipal user,
        int? limit = null,
        CancellationToken cancellationToken = default);

    Task<ProfileBudgetResponse> CreateAsync(
        ClaimsPrincipal user,
        CreateProfileBudgetRequest request,
        CancellationToken cancellationToken = default);

    Task<ProfileBudgetResponse> UpdateAsync(
        ClaimsPrincipal user,
        Guid id,
        UpdateProfileBudgetRequest request,
        CancellationToken cancellationToken = default);

    Task<ProfileBudgetResponse> UpdateActiveAsync(
        ClaimsPrincipal user,
        Guid id,
        UpdateProfileBudgetActiveRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        ClaimsPrincipal user,
        Guid id,
        CancellationToken cancellationToken = default);
}
