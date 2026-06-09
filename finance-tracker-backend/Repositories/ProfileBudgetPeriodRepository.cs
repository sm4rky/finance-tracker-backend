using finance_tracker_backend.Models;
using Microsoft.Extensions.Configuration;
using Npgsql;
using NpgsqlTypes;

namespace finance_tracker_backend.Repositories;

public sealed class ProfileBudgetPeriodRepository(
    Supabase.Client supabaseClient,
    IConfiguration configuration) : IProfileBudgetPeriodRepository
{
    public async Task<ProfileBudgetPeriod?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await supabaseClient.From<ProfileBudgetPeriod>()
            .Where(p => p.Id == id)
            .Get(cancellationToken)
            .ConfigureAwait(false);

        return result.Models.Count > 0 ? result.Models[0] : null;
    }

    public async Task<ProfileBudgetPeriod?> GetByBudgetAndRangeAsync(
        Guid budgetId,
        DateOnly periodStartDate,
        DateOnly periodEndDate,
        CancellationToken cancellationToken = default)
    {
        var result = await supabaseClient.From<ProfileBudgetPeriod>()
            .Where(p => p.BudgetId == budgetId)
            .Where(p => p.PeriodStartDate == periodStartDate)
            .Where(p => p.PeriodEndDate == periodEndDate)
            .Get(cancellationToken)
            .ConfigureAwait(false);

        return result.Models.Count > 0 ? result.Models[0] : null;
    }

    public async Task<IReadOnlyList<ProfileBudgetPeriod>> ListByBudgetIdAsync(
        Guid budgetId,
        CancellationToken cancellationToken = default)
    {
        var result = await supabaseClient.From<ProfileBudgetPeriod>()
            .Where(p => p.BudgetId == budgetId)
            .Get(cancellationToken)
            .ConfigureAwait(false);

        return result.Models
            .OrderByDescending(p => p.PeriodStartDate)
            .ThenByDescending(p => p.PeriodEndDate)
            .ToList();
    }

    public async Task<ProfileBudgetPeriod?> GetCurrentPeriodAsync(
        Guid budgetId,
        DateOnly today,
        CancellationToken cancellationToken = default)
    {
        const string sql =
            """
            SELECT id, budget_id, period_start_date, period_end_date, period_name,
                   amount_limit, spent_amount, created_at, updated_at
            FROM profile_budget_periods
            WHERE budget_id = @budget_id
              AND period_start_date <= @today
              AND period_end_date >= @today
            ORDER BY period_start_date DESC
            LIMIT 1
            """;

        await using var conn = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("budget_id", budgetId);
        cmd.Parameters.Add(new NpgsqlParameter("today", NpgsqlDbType.Date) { Value = today });

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        return await reader.ReadAsync(cancellationToken).ConfigureAwait(false)
            ? MapPeriod(reader)
            : null;
    }

    public async Task<IReadOnlyList<ProfileBudgetPeriod>> ListActiveBudgetPeriodsContainingAnyDateAsync(
        Guid profileId,
        IReadOnlyCollection<DateOnly> dates,
        CancellationToken cancellationToken = default)
    {
        if (dates.Count == 0)
            return [];

        const string sql =
            """
            SELECT p.id, p.budget_id, p.period_start_date, p.period_end_date, p.period_name,
                   p.amount_limit, p.spent_amount, p.created_at, p.updated_at
            FROM profile_budget_periods p
            INNER JOIN profile_budgets b ON b.id = p.budget_id
            WHERE b.profile_id = @profile_id
              AND b.is_active = TRUE
              AND EXISTS (
                  SELECT 1
                  FROM unnest(@dates::date[]) AS d(value)
                  WHERE p.period_start_date <= d.value
                    AND p.period_end_date >= d.value
              )
            ORDER BY p.period_start_date DESC, p.period_end_date DESC
            """;

        await using var conn = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("profile_id", profileId);
        cmd.Parameters.Add(new NpgsqlParameter("dates", NpgsqlDbType.Array | NpgsqlDbType.Date)
        {
            Value = dates.Distinct().ToArray()
        });

        var periods = new List<ProfileBudgetPeriod>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            periods.Add(MapPeriod(reader));

        return periods;
    }

    public async Task InsertAsync(ProfileBudgetPeriod period, CancellationToken cancellationToken = default)
    {
        await supabaseClient.From<ProfileBudgetPeriod>()
            .Insert(period, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task UpdateSpentAmountAsync(
        Guid periodId,
        decimal spentAmount,
        DateTimeOffset updatedAt,
        CancellationToken cancellationToken = default)
    {
        await supabaseClient.From<ProfileBudgetPeriod>()
            .Where(p => p.Id == periodId)
            .Set(p => p.SpentAmount, spentAmount)
            .Set(p => p.UpdatedAt, updatedAt)
            .Update(null, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task UpdateAmountLimitAsync(
        Guid periodId,
        decimal amountLimit,
        DateTimeOffset updatedAt,
        CancellationToken cancellationToken = default)
    {
        await supabaseClient.From<ProfileBudgetPeriod>()
            .Where(p => p.Id == periodId)
            .Set(p => p.AmountLimit, amountLimit)
            .Set(p => p.UpdatedAt, updatedAt)
            .Update(null, cancellationToken)
            .ConfigureAwait(false);
    }

    private static ProfileBudgetPeriod MapPeriod(NpgsqlDataReader reader) => new()
    {
        Id = reader.GetGuid(0),
        BudgetId = reader.GetGuid(1),
        PeriodStartDate = DateOnly.FromDateTime(reader.GetDateTime(2)),
        PeriodEndDate = DateOnly.FromDateTime(reader.GetDateTime(3)),
        PeriodName = reader.GetString(4),
        AmountLimit = reader.GetDecimal(5),
        SpentAmount = reader.GetDecimal(6),
        CreatedAt = reader.GetFieldValue<DateTimeOffset>(7),
        UpdatedAt = reader.GetFieldValue<DateTimeOffset>(8)
    };

    private async Task<NpgsqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connectionString = configuration.GetConnectionString("Default");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:Default is required for profile budget period queries.");
        }

        var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(cancellationToken).ConfigureAwait(false);
        return conn;
    }
}
