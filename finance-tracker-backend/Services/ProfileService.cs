using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;
using finance_tracker_backend.Middleware;
using finance_tracker_backend.Models;
using finance_tracker_backend.Repositories;

namespace finance_tracker_backend.Services;

public sealed partial class ProfileService(
    IProfileRepository profileRepository,
    IProfileNotificationPreferenceRepository notificationPreferenceRepository) : IProfileService
{
    [GeneratedRegex("^[a-zA-Z0-9._]{8,30}$", RegexOptions.Compiled)]
    private static partial Regex UsernameRegex();

    [GeneratedRegex(@"^[a-zA-Z0-9._/-]+$", RegexOptions.Compiled)]
    private static partial Regex AvatarStoragePathSuffixRegex();
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

        var now = DateTimeOffset.UtcNow;
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
        await notificationPreferenceRepository.InsertDefaultAsync(userId, cancellationToken).ConfigureAwait(false);
        return true;
    }

    public Task<Profile?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        profileRepository.GetByIdAsync(userId, cancellationToken);

    public async Task SetUsernameAsync(ClaimsPrincipal user, string username,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(username);

        var trimmed = username.Trim();
        if (string.IsNullOrEmpty(trimmed))
            throw new ArgumentException("Username is required.");

        var normalized = trimmed.ToLowerInvariant();

        if (!UsernameRegex().IsMatch(normalized))
            throw new ArgumentException(
                "Username must be 8–30 characters and contain only letters, numbers, periods, and underscores.");

        var userId = RequireUserId(user);

        var profile = await profileRepository.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        if (profile is null)
            throw new InvalidOperationException("Profile was not found.");

        if (!string.IsNullOrWhiteSpace(profile.Username))
            throw new UsernameImmutableException("Username cannot be changed once it has been set.");

        var existing =
            await profileRepository.GetByUsernameAsync(normalized, cancellationToken)
                .ConfigureAwait(false);
        if (existing is not null && existing.Id != userId)
            throw new UsernameTakenException("This username is already taken.");

        await profileRepository.UpdateUsernameAsync(userId, normalized, cancellationToken).ConfigureAwait(false);
    }

    public async Task MarkPasswordLoginEnabledAsync(ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        var userId = RequireUserId(user);
        var profile = await profileRepository.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        if (profile is null)
            throw new InvalidOperationException("Profile was not found.");

        await profileRepository.UpdatePasswordLoginEnabledAsync(userId, true, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task SetAvatarUrlAsync(ClaimsPrincipal user, string? avatarUrl,
        CancellationToken cancellationToken = default)
    {
        var userId = RequireUserId(user);

        var profile = await profileRepository.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        if (profile is null)
            throw new InvalidOperationException("Profile was not found.");

        string? stored = null;
        if (!string.IsNullOrWhiteSpace(avatarUrl))
        {
            var trimmed = avatarUrl.Trim();

            var expectedPrefix = $"{userId:D}/";
            if (!trimmed.StartsWith(expectedPrefix, StringComparison.Ordinal))
                throw new ArgumentException(
                    "Avatar URL must be a storage path starting with your user id, e.g. \"{your-user-id}/avatar.webp\".");

            if (trimmed.Contains("..", StringComparison.Ordinal))
                throw new ArgumentException("Invalid avatar path.");

            var suffix = trimmed.AsSpan(expectedPrefix.Length);
            if (suffix.Length == 0 || !AvatarStoragePathSuffixRegex().IsMatch(suffix.ToString()))
                throw new ArgumentException("Avatar path contains invalid characters.");

            stored = trimmed;
        }

        await profileRepository.UpdateAvatarUrlAsync(userId, stored, cancellationToken).ConfigureAwait(false);
    }

    private static Guid RequireUserId(ClaimsPrincipal user)
    {
        var sub = user.FindFirstValue("sub") ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(sub) || !Guid.TryParse(sub, out var userId))
            throw new InvalidOperationException("Authenticated user is missing a valid 'sub' claim.");
        return userId;
    }
}
