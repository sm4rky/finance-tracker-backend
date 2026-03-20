using System.Security.Claims;
using finance_tracker_backend.Models;

namespace finance_tracker_backend.Services;

public interface IProfileSubscriptionService
{
    Task EnsureDefaultFreePlanExistsAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default);

    Task<ProfileSubscription?> GetForProfileAsync(Guid profileId, CancellationToken cancellationToken = default);
}
