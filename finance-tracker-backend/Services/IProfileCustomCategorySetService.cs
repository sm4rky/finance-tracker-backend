using System.Security.Claims;
using finance_tracker_backend.Contracts.Requests;
using finance_tracker_backend.Contracts.Responses;

namespace finance_tracker_backend.Services;

public interface IProfileCustomCategorySetService
{
    Task<IReadOnlyList<ProfileCustomCategorySetResponse>> ListAsync(
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default);

    Task<ProfileCustomCategorySetResponse> UpsertAsync(
        ClaimsPrincipal user,
        UpsertProfileCustomCategorySetRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        ClaimsPrincipal user,
        Guid id,
        CancellationToken cancellationToken = default);
}
