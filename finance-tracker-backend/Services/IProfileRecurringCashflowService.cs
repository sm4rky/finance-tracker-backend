using System.Security.Claims;
using finance_tracker_backend.Contracts.Requests;
using finance_tracker_backend.Contracts.Responses;

namespace finance_tracker_backend.Services;

public interface IProfileRecurringCashflowService
{
    Task<IReadOnlyList<ProfileRecurringCashflowResponse>> ListAsync(
        ClaimsPrincipal user,
        string? status,
        CancellationToken cancellationToken = default);

    Task<ProfileRecurringCashflowResponse> GetByIdAsync(
        ClaimsPrincipal user,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<ProfileRecurringCashflowResponse> CreateAsync(
        ClaimsPrincipal user,
        SaveProfileRecurringCashflowRequest request,
        CancellationToken cancellationToken = default);

    Task<ProfileRecurringCashflowResponse> UpdateAsync(
        ClaimsPrincipal user,
        Guid id,
        SaveProfileRecurringCashflowRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(ClaimsPrincipal user, Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProfileRecurringCashflowCalendarOccurrenceResponse>> GetCalendarAsync(
        ClaimsPrincipal user,
        DateOnly dateFrom,
        DateOnly dateTo,
        CancellationToken cancellationToken = default);
}
