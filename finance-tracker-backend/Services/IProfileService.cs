using System.Security.Claims;
using finance_tracker_backend.Models;

namespace finance_tracker_backend.Services;

public interface IProfileService
{
    Task<bool> EnsureRecordExistsAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default);

    Task<Profile?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task SetUsernameAsync(ClaimsPrincipal user, string username, CancellationToken cancellationToken = default);

    Task MarkPasswordLoginEnabledAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default);
}
