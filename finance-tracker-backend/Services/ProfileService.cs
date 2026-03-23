using System.Security.Claims;
using System.Text.Json;
using finance_tracker_backend.Models;
using finance_tracker_backend.Repositories;

namespace finance_tracker_backend.Services;

public sealed class ProfileService(IProfileRepository profileRepository) : IProfileService
{
    public async Task<bool> EnsureRecordExistsAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        var userId = RequireUserId(user);
        if (await profileRepository.ExistsAsync(userId, cancellationToken).ConfigureAwait(false))
            return false;

        var emailRaw = user.FindFirstValue("email");
        var email = string.IsNullOrWhiteSpace(emailRaw) ? null : emailRaw.Trim();

        string? fullName = null;
        var meta = user.FindFirstValue("user_metadata");
        if (!string.IsNullOrWhiteSpace(meta))
            try
            {
                using var doc = JsonDocument.Parse(meta);
                if (doc.RootElement.TryGetProperty("full_name", out var el) && el.ValueKind == JsonValueKind.String)
                {
                    var s = el.GetString();
                    if (!string.IsNullOrWhiteSpace(s)) fullName = s.Trim();
                }
            }
            catch (JsonException) { }

        var now = DateTime.UtcNow;
        await profileRepository.InsertAsync(
            new Profile
            {
                Id = userId,
                Email = email,
                FullName = fullName,
                Role = "user",
                CreatedAt = now,
                UpdatedAt = now
            },
            cancellationToken).ConfigureAwait(false);
        return true;
    }

    public Task<Profile?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        profileRepository.GetByIdAsync(userId, cancellationToken);

    private static Guid RequireUserId(ClaimsPrincipal user)
    {
        var sub = user.FindFirstValue("sub") ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(sub) || !Guid.TryParse(sub, out var userId))
            throw new InvalidOperationException("Authenticated user is missing a valid 'sub' claim.");
        return userId;
    }
}
