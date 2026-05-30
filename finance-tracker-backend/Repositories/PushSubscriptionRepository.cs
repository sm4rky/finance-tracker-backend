using finance_tracker_backend.Models;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace finance_tracker_backend.Repositories;

public sealed class PushSubscriptionRepository(
    Supabase.Client supabaseClient,
    IConfiguration configuration) : IPushSubscriptionRepository
{
    public async Task<IReadOnlyList<PushSubscription>> ListByProfileIdAsync(
        Guid profileId,
        CancellationToken cancellationToken = default)
    {
        var result = await supabaseClient.From<PushSubscription>()
            .Where(x => x.ProfileId == profileId)
            .Get(cancellationToken)
            .ConfigureAwait(false);
        return result.Models
            .OrderByDescending(x => x.UpdatedAt)
            .ThenBy(x => x.Id)
            .ToList();
    }

    public async Task<PushSubscription?> GetByIdForProfileAsync(
        Guid profileId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var result = await supabaseClient.From<PushSubscription>()
            .Where(x => x.Id == id)
            .Where(x => x.ProfileId == profileId)
            .Get(cancellationToken)
            .ConfigureAwait(false);
        return result.Models.Count > 0 ? result.Models[0] : null;
    }

    public async Task<PushSubscription> UpsertByEndpointAsync(
        PushSubscription subscription,
        CancellationToken cancellationToken = default)
    {
        const string sql =
            """
            INSERT INTO push_subscriptions (
                id,
                profile_id,
                endpoint,
                p256dh,
                auth,
                user_agent,
                created_at,
                updated_at
            )
            VALUES (
                @id,
                @profile_id,
                @endpoint,
                @p256dh,
                @auth,
                @user_agent,
                @now,
                @now
            )
            ON CONFLICT (endpoint) DO UPDATE SET
                profile_id = EXCLUDED.profile_id,
                p256dh = EXCLUDED.p256dh,
                auth = EXCLUDED.auth,
                user_agent = EXCLUDED.user_agent,
                updated_at = EXCLUDED.updated_at
            RETURNING id, profile_id, endpoint, p256dh, auth, user_agent,
                      created_at, updated_at;
            """;

        var now = DateTimeOffset.UtcNow;
        await using var conn = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", subscription.Id == Guid.Empty ? Guid.NewGuid() : subscription.Id);
        cmd.Parameters.AddWithValue("profile_id", subscription.ProfileId);
        cmd.Parameters.AddWithValue("endpoint", subscription.Endpoint);
        cmd.Parameters.AddWithValue("p256dh", subscription.P256dh);
        cmd.Parameters.AddWithValue("auth", subscription.Auth);
        cmd.Parameters.AddWithValue("user_agent", (object?)subscription.UserAgent ?? DBNull.Value);
        cmd.Parameters.AddWithValue("now", now);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            return Map(reader);

        throw new InvalidOperationException("Push subscription upsert did not return a row.");
    }

    public async Task<int> DeleteForProfileAsync(
        Guid profileId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        const string sql =
            """
            DELETE FROM push_subscriptions
            WHERE profile_id = @profile_id AND id = @id
            """;

        await using var conn = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("profile_id", profileId);
        cmd.Parameters.AddWithValue("id", id);
        return await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<NpgsqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connectionString = configuration.GetConnectionString("Default");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("ConnectionStrings:Default is required for push subscription upserts.");

        var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(cancellationToken).ConfigureAwait(false);
        return conn;
    }

    private static PushSubscription Map(NpgsqlDataReader reader) => new()
    {
        Id = reader.GetGuid(0),
        ProfileId = reader.GetGuid(1),
        Endpoint = reader.GetString(2),
        P256dh = reader.GetString(3),
        Auth = reader.GetString(4),
        UserAgent = reader.IsDBNull(5) ? null : reader.GetString(5),
        CreatedAt = reader.GetFieldValue<DateTimeOffset>(6),
        UpdatedAt = reader.GetFieldValue<DateTimeOffset>(7)
    };
}
