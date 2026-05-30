using finance_tracker_backend.Models;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace finance_tracker_backend.Repositories;

public sealed class ProfileNotificationPreferenceRepository(
    Supabase.Client supabaseClient,
    IConfiguration configuration)
    : IProfileNotificationPreferenceRepository
{
    public async Task<ProfileNotificationPreference?> GetByProfileIdAsync(
        Guid profileId,
        CancellationToken cancellationToken = default)
    {
        var result = await supabaseClient.From<ProfileNotificationPreference>()
            .Where(x => x.ProfileId == profileId)
            .Get(cancellationToken)
            .ConfigureAwait(false);
        return result.Models.Count > 0 ? result.Models[0] : null;
    }

    public async Task InsertDefaultAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
        const string sql =
            """
            INSERT INTO profile_notification_preferences (
                id,
                profile_id,
                email_enabled,
                due_reminder_enabled,
                reminder_days_before,
                budget_alert_enabled,
                budget_alert_threshold,
                monthly_statement_enabled,
                created_at,
                updated_at
            )
            VALUES (
                @id,
                @profile_id,
                FALSE,
                FALSE,
                1,
                FALSE,
                80,
                FALSE,
                @now,
                @now
            )
            ON CONFLICT (profile_id) DO NOTHING;
            """;

        await using var conn = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", Guid.NewGuid());
        cmd.Parameters.AddWithValue("profile_id", profileId);
        cmd.Parameters.AddWithValue("now", DateTimeOffset.UtcNow);
        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateAsync(ProfileNotificationPreference row, CancellationToken cancellationToken = default)
    {
        await supabaseClient.From<ProfileNotificationPreference>()
            .Where(x => x.Id == row.Id)
            .Where(x => x.ProfileId == row.ProfileId)
            .Set(x => x.EmailEnabled, row.EmailEnabled)
            .Set(x => x.DueReminderEnabled, row.DueReminderEnabled)
            .Set(x => x.ReminderDaysBefore, row.ReminderDaysBefore)
            .Set(x => x.BudgetAlertEnabled, row.BudgetAlertEnabled)
            .Set(x => x.BudgetAlertThreshold, row.BudgetAlertThreshold)
            .Set(x => x.MonthlyStatementEnabled, row.MonthlyStatementEnabled)
            .Set(x => x.UpdatedAt, row.UpdatedAt)
            .Update(null, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<NpgsqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connectionString = configuration.GetConnectionString("Default");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:Default is required for notification preference inserts.");
        }

        var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(cancellationToken).ConfigureAwait(false);
        return conn;
    }
}
