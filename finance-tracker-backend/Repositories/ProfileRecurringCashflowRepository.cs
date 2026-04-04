using finance_tracker_backend.Models;
using Microsoft.Extensions.Configuration;
using Npgsql;
using NpgsqlTypes;

namespace finance_tracker_backend.Repositories;

public sealed class ProfileRecurringCashflowRepository(
    Supabase.Client supabaseClient,
    IConfiguration configuration) : IProfileRecurringCashflowRepository
{
    public async Task<ProfileRecurringCashflow?> GetByIdForProfileAsync(
        Guid profileId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var result = await supabaseClient
            .From<ProfileRecurringCashflow>()
            .Where(r => r.ProfileId == profileId)
            .Where(r => r.Id == id)
            .Get(cancellationToken)
            .ConfigureAwait(false);

        return result.Models.Count > 0 ? result.Models[0] : null;
    }

    public async Task<ProfileRecurringCashflow?> GetByProfileAndPlaidStreamIdAsync(
        Guid profileId,
        string plaidStreamId,
        CancellationToken cancellationToken = default)
    {
        var result = await supabaseClient
            .From<ProfileRecurringCashflow>()
            .Where(r => r.ProfileId == profileId)
            .Where(r => r.PlaidStreamId == plaidStreamId)
            .Get(cancellationToken)
            .ConfigureAwait(false);

        return result.Models.Count > 0 ? result.Models[0] : null;
    }

    public async Task<IReadOnlyList<ProfileRecurringCashflow>> ListForProfileAsync(
        Guid profileId,
        string? status,
        CancellationToken cancellationToken = default)
    {
        const string sql =
            """
            SELECT
                id,
                profile_id,
                plaid_stream_id,
                linked_bank_account_id,
                direction,
                merchant_name,
                description,
                pfc_primary,
                pfc_detailed,
                frequency,
                last_amount,
                expected_amount,
                expected_amount_user_set,
                first_date,
                last_date,
                predicted_next_date,
                status,
                created_at,
                updated_at
            FROM profile_recurring_cashflow
            WHERE profile_id = @profile_id
              AND (@status_filter IS NULL OR status = @status_filter)
            ORDER BY
                predicted_next_date ASC NULLS LAST,
                updated_at DESC
            """;

        await using var conn = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var cmd = new NpgsqlCommand(sql, conn);

        cmd.Parameters.AddWithValue("profile_id", profileId);
        cmd.Parameters.Add(
            new NpgsqlParameter("status_filter", NpgsqlDbType.Text)
            {
                Value = string.IsNullOrWhiteSpace(status) ? DBNull.Value : status.Trim()
            });

        var list = new List<ProfileRecurringCashflow>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            list.Add(MapRow(reader));
        }

        return list;
    }

    public async Task InsertAsync(ProfileRecurringCashflow row, CancellationToken cancellationToken = default)
    {
        await supabaseClient
            .From<ProfileRecurringCashflow>()
            .Insert(row, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task UpdateAsync(ProfileRecurringCashflow row, CancellationToken cancellationToken = default)
    {
        await supabaseClient
            .From<ProfileRecurringCashflow>()
            .Where(r => r.Id == row.Id)
            .Where(r => r.ProfileId == row.ProfileId)
            .Set(r => r.PlaidStreamId!, row.PlaidStreamId)
            .Set(r => r.LinkedBankAccountId!, row.LinkedBankAccountId)
            .Set(r => r.Direction, row.Direction)
            .Set(r => r.MerchantName!, row.MerchantName)
            .Set(r => r.Description!, row.Description)
            .Set(r => r.PfcPrimary!, row.PfcPrimary)
            .Set(r => r.PfcDetailed!, row.PfcDetailed)
            .Set(r => r.Frequency, row.Frequency)
            .Set(r => r.LastAmount!, row.LastAmount)
            .Set(r => r.ExpectedAmount, row.ExpectedAmount)
            .Set(r => r.ExpectedAmountUserSet, row.ExpectedAmountUserSet)
            .Set(r => r.FirstDate!, row.FirstDate)
            .Set(r => r.LastDate!, row.LastDate)
            .Set(r => r.PredictedNextDate!, row.PredictedNextDate)
            .Set(r => r.Status, row.Status)
            .Set(r => r.UpdatedAt, row.UpdatedAt)
            .Update(null, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<int> DeleteForProfileAsync(
        Guid profileId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        const string sql =
            """
            DELETE FROM profile_recurring_cashflow
            WHERE profile_id = @profile_id AND id = @id
            """;

        await using var conn = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var cmd = new NpgsqlCommand(sql, conn);

        cmd.Parameters.AddWithValue("profile_id", profileId);
        cmd.Parameters.AddWithValue("id", id);

        return await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<int> SetStatusForLinkedBankAccountAsync(
        Guid profileId,
        Guid linkedBankAccountId,
        string status,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(status))
            throw new ArgumentException("status is required.");

        var normalized = status.Trim().ToLowerInvariant();
        if (normalized is not ("active" or "inactive" or "unlinked"))
            throw new ArgumentException("status must be 'active', 'inactive', or 'unlinked'.");

        var updatedAt = DateTimeOffset.UtcNow;

        const string sql =
            """
            UPDATE profile_recurring_cashflow
            SET status = @status,
                linked_bank_account_id = NULL,
                updated_at = @updated_at
            WHERE profile_id = @profile_id
              AND linked_bank_account_id = @linked_bank_account_id
            """;

        await using var conn = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var cmd = new NpgsqlCommand(sql, conn);

        cmd.Parameters.AddWithValue("profile_id", profileId);
        cmd.Parameters.AddWithValue("linked_bank_account_id", linkedBankAccountId);
        cmd.Parameters.AddWithValue("status", normalized);
        cmd.Parameters.AddWithValue("updated_at", updatedAt);

        return await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<NpgsqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connectionString = configuration.GetConnectionString("Default");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:Default is required for profile recurring cashflow queries.");
        }

        var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(cancellationToken).ConfigureAwait(false);
        return conn;
    }

    private static ProfileRecurringCashflow MapRow(NpgsqlDataReader reader) => new()
    {
        Id = reader.GetGuid(0),
        ProfileId = reader.GetGuid(1),
        PlaidStreamId = reader.IsDBNull(2) ? null : reader.GetString(2),
        LinkedBankAccountId = reader.IsDBNull(3) ? null : reader.GetGuid(3),
        Direction = reader.GetString(4),
        MerchantName = reader.IsDBNull(5) ? null : reader.GetString(5),
        Description = reader.IsDBNull(6) ? null : reader.GetString(6),
        PfcPrimary = reader.IsDBNull(7) ? null : reader.GetString(7),
        PfcDetailed = reader.IsDBNull(8) ? null : reader.GetString(8),
        Frequency = reader.GetString(9),
        LastAmount = reader.IsDBNull(10) ? null : reader.GetDecimal(10),
        ExpectedAmount = reader.GetDecimal(11),
        ExpectedAmountUserSet = reader.GetBoolean(12),
        FirstDate = reader.IsDBNull(13) ? null : reader.GetFieldValue<DateOnly>(13),
        LastDate = reader.IsDBNull(14) ? null : reader.GetFieldValue<DateOnly>(14),
        PredictedNextDate = reader.IsDBNull(15) ? null : reader.GetFieldValue<DateOnly>(15),
        Status = reader.GetString(16),
        CreatedAt = reader.GetFieldValue<DateTimeOffset>(17),
        UpdatedAt = reader.GetFieldValue<DateTimeOffset>(18)
    };
}
