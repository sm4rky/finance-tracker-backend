using finance_tracker_backend.Models;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Supabase.Postgrest;

namespace finance_tracker_backend.Repositories;

public sealed class ProfileRepository(Supabase.Client supabaseClient, IConfiguration configuration)
    : IProfileRepository
{
    public async Task<bool> ExistsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var result = await supabaseClient.From<Profile>().Where(p => p.Id == userId).Get(cancellationToken)
            .ConfigureAwait(false);
        return result.Models.Count > 0;
    }

    public async Task<Profile?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var result = await supabaseClient.From<Profile>().Where(p => p.Id == userId).Get(cancellationToken)
            .ConfigureAwait(false);
        return result.Models.Count > 0 ? result.Models[0] : null;
    }

    public async Task<Profile?> GetByUsernameAsync(string username,
        CancellationToken cancellationToken = default)
    {
        var result = await supabaseClient.From<Profile>()
            .Where(p => p.Username == username)
            .Get(cancellationToken)
            .ConfigureAwait(false);
        return result.Models.Count > 0 ? result.Models[0] : null;
    }

    public async Task InsertAsync(Profile profile, CancellationToken cancellationToken = default)
    {
        var options = new QueryOptions
        {
            DuplicateResolution = QueryOptions.DuplicateResolutionType.IgnoreDuplicates
        };
        await supabaseClient.From<Profile>().Insert(profile, options, cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateUsernameAsync(Guid profileId, string username,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        await supabaseClient.From<Profile>()
            .Where(p => p.Id == profileId)
            .Set(p => p.Username!, username)
            .Set(p => p.UpdatedAt, now)
            .Update(null, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task UpdatePasswordLoginEnabledAsync(Guid profileId, bool enabled,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        await supabaseClient.From<Profile>()
            .Where(p => p.Id == profileId)
            .Set(p => p.PasswordLoginEnabled, enabled)
            .Set(p => p.UpdatedAt, now)
            .Update(null, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task UpdateAvatarUrlAsync(Guid profileId, string? avatarUrl,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        await supabaseClient.From<Profile>()
            .Where(p => p.Id == profileId)
            .Set(p => p.AvatarUrl!, avatarUrl!)
            .Set(p => p.UpdatedAt, now)
            .Update(null, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Guid>> ListAllProfileIdsAsync(CancellationToken cancellationToken = default)
    {
        var result = await supabaseClient.From<Profile>().Get(cancellationToken).ConfigureAwait(false);
        return result.Models.Select(p => p.Id).ToList();
    }

    public async Task<IReadOnlyList<Guid>> ListProfileIdsAfterIdAsync(
        Guid afterProfileId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        if (limit < 1)
            throw new ArgumentOutOfRangeException(nameof(limit), limit, "limit must be at least 1.");

        const string sql =
            """
            SELECT id
            FROM profiles
            WHERE id > @after_profile_id
            ORDER BY id ASC
            LIMIT @limit
            """;

        await using var conn = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("after_profile_id", afterProfileId);
        cmd.Parameters.AddWithValue("limit", limit);

        var list = new List<Guid>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            list.Add(reader.GetGuid(0));

        return list;
    }

    private async Task<NpgsqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connectionString = configuration.GetConnectionString("Default");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:Default is required for profile id batch queries.");
        }

        var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(cancellationToken).ConfigureAwait(false);
        return conn;
    }
}
