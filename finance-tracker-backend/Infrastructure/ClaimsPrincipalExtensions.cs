using System.Security.Claims;

namespace finance_tracker_backend.Infrastructure;

public static class ClaimsPrincipalExtensions
{
    public static Guid RequireProfileId(this ClaimsPrincipal user)
    {
        var sub = user.FindFirstValue("sub") ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(sub) || !Guid.TryParse(sub, out var profileId))
            throw new InvalidOperationException("Authenticated user is missing a valid 'sub' claim.");
        return profileId;
    }
}
