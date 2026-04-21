using System.Text;
using Npgsql;

namespace finance_tracker_backend.Repositories;

public sealed class ProfileMonthlyNetWorthRepository(IConfiguration configuration)
    : IProfileMonthlyNetWorthRepository
{
    public async Task UpsertAsync(
        Guid profileId,
        DateOnly periodStartDate,
        decimal totalAssets,
        decimal totalLiabilities,
        decimal netWorth,
        DateTimeOffset createdAt,
        CancellationToken cancellationToken = default)
    {
        var connectionString = configuration.GetConnectionString("Default");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:Default is required for profile monthly net worth.");
        }

        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(cancellationToken).ConfigureAwait(false);

        const string sql =
            """
            INSERT INTO profile_monthly_net_worth (
                id,
                profile_id,
                period_start_date,
                total_assets,
                total_liabilities,
                net_worth,
                created_at)
            VALUES (
                @id,
                @profile_id,
                @period_start_date,
                @total_assets,
                @total_liabilities,
                @net_worth,
                @created_at)
            ON CONFLICT (profile_id, period_start_date)
            DO UPDATE SET
                total_assets = EXCLUDED.total_assets,
                total_liabilities = EXCLUDED.total_liabilities,
                net_worth = EXCLUDED.net_worth,
                created_at = EXCLUDED.created_at;
            """;

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", Guid.NewGuid());
        cmd.Parameters.AddWithValue("profile_id", profileId);
        cmd.Parameters.AddWithValue("period_start_date", periodStartDate);
        cmd.Parameters.AddWithValue("total_assets", totalAssets);
        cmd.Parameters.AddWithValue("total_liabilities", totalLiabilities);
        cmd.Parameters.AddWithValue("net_worth", netWorth);
        cmd.Parameters.AddWithValue("created_at", createdAt);

        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<(DateOnly PeriodStartDate, decimal TotalAssets, decimal TotalLiabilities, decimal NetWorth, DateTimeOffset CreatedAt)>>
        ListByProfileIdAsync(
            Guid profileId,
            DateOnly? fromInclusive,
            DateOnly? toInclusive,
            CancellationToken cancellationToken = default)
    {
        var connectionString = configuration.GetConnectionString("Default");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:Default is required for profile monthly net worth.");
        }

        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(cancellationToken).ConfigureAwait(false);

        var sql = new StringBuilder(
            """
            SELECT period_start_date, total_assets, total_liabilities, net_worth, created_at
            FROM profile_monthly_net_worth
            WHERE profile_id = @profile_id
            """);

        await using var cmd = new NpgsqlCommand(null, conn);
        cmd.Parameters.AddWithValue("profile_id", profileId);

        if (fromInclusive.HasValue)
        {
            sql.Append(" AND period_start_date >= @from_inclusive");
            cmd.Parameters.AddWithValue("from_inclusive", fromInclusive.Value);
        }

        if (toInclusive.HasValue)
        {
            sql.Append(" AND period_start_date <= @to_inclusive");
            cmd.Parameters.AddWithValue("to_inclusive", toInclusive.Value);
        }

        sql.Append(" ORDER BY period_start_date ASC;");
        cmd.CommandText = sql.ToString();

        var rows = new List<(DateOnly, decimal, decimal, decimal, DateTimeOffset)>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var period = reader.GetFieldValue<DateOnly>(0);
            var assets = reader.GetDecimal(1);
            var liabilities = reader.GetDecimal(2);
            var net = reader.GetDecimal(3);
            var created = reader.GetFieldValue<DateTimeOffset>(4);
            rows.Add((period, assets, liabilities, net, created));
        }

        return rows;
    }
}
