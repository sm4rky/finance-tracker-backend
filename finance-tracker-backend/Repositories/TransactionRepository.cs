using System.Globalization;
using System.Text;
using finance_tracker_backend.Enums;
using finance_tracker_backend.Models;
using finance_tracker_backend.Types;
using Npgsql;
using NpgsqlTypes;

namespace finance_tracker_backend.Repositories;

public sealed class TransactionRepository(
    Supabase.Client supabaseClient,
    IConfiguration configuration) : ITransactionRepository
{
    public async Task<Transaction?> GetByIdForProfileAsync(
        Guid profileId,
        Guid transactionId,
        CancellationToken cancellationToken = default)
    {
        var result = await supabaseClient
            .From<Transaction>()
            .Where(t => t.ProfileId == profileId)
            .Where(t => t.Id == transactionId)
            .Get(cancellationToken)
            .ConfigureAwait(false);

        return result.Models.Count > 0 ? result.Models[0] : null;
    }

    public async Task<Transaction?> GetByProfileAndPlaidTransactionIdAsync(
        Guid profileId,
        string plaidTransactionId,
        CancellationToken cancellationToken = default)
    {
        var result = await supabaseClient
            .From<Transaction>()
            .Where(t => t.ProfileId == profileId)
            .Where(t => t.PlaidTransactionId == plaidTransactionId)
            .Get(cancellationToken)
            .ConfigureAwait(false);

        return result.Models.Count > 0 ? result.Models[0] : null;
    }

    public async Task<Transaction?> FindActiveDuplicateForFingerprintAsync(
        Guid profileId,
        DateOnly date,
        decimal amount,
        string normalizedMerchantFingerprint,
        string incomingPlaidTransactionId,
        CancellationToken cancellationToken = default)
    {
        const string sql =
            """
            SELECT
                t.id,
                t.profile_id,
                t.linked_bank_account_id,
                t.plaid_transaction_id,
                t.amount,
                t.iso_currency_code,
                t.date,
                t.authorized_date,
                t.authorized_datetime,
                t.name,
                t.merchant_name,
                t.merchant_entity_id,
                t.pending,
                t.pending_transaction_id,
                t.payment_channel,
                t.transaction_type,
                t.pfc_primary,
                t.pfc_detailed,
                t.pfc_confidence_level,
                t.pfc_version,
                t.logo_url,
                t.website,
                t.status,
                t.removed_at,
                t.created_at,
                t.updated_at
            FROM transactions t
            WHERE t.profile_id = @profile_id
              AND t.removed_at IS NULL
              AND t.date = @date
              AND t.amount = @amount
              AND lower(trim(both from regexp_replace(coalesce(t.merchant_name, t.name), '\s+', ' ', 'g'))) = @norm_merchant
              AND t.plaid_transaction_id <> @incoming_plaid_id
              AND (
                  SELECT COUNT(*)::bigint
                  FROM transactions t2
                  WHERE t2.profile_id = @profile_id
                    AND t2.removed_at IS NULL
                    AND t2.date = @date
                    AND t2.amount = @amount
                    AND lower(trim(both from regexp_replace(coalesce(t2.merchant_name, t2.name), '\s+', ' ', 'g'))) = @norm_merchant
                    AND t2.plaid_transaction_id <> @incoming_plaid_id
              ) = 1;
            """;

        await using var conn = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var cmd = new NpgsqlCommand(sql, conn);

        cmd.Parameters.AddWithValue("profile_id", profileId);
        cmd.Parameters.Add(new NpgsqlParameter("date", NpgsqlDbType.Date) { Value = date });
        cmd.Parameters.AddWithValue("amount", amount);
        cmd.Parameters.AddWithValue("norm_merchant", normalizedMerchantFingerprint);
        cmd.Parameters.AddWithValue("incoming_plaid_id", incomingPlaidTransactionId);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            return null;

        return MapTransaction(reader);
    }

    public async Task InsertAsync(
        Transaction transaction,
        CancellationToken cancellationToken = default)
    {
        await supabaseClient
            .From<Transaction>()
            .Insert(transaction, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task UpdateAsync(
        Transaction transaction,
        CancellationToken cancellationToken = default)
    {
        await supabaseClient
            .From<Transaction>()
            .Where(t => t.Id == transaction.Id)
            .Where(t => t.ProfileId == transaction.ProfileId)
            .Set(t => t.LinkedBankAccountId!, transaction.LinkedBankAccountId)
            .Set(t => t.PlaidTransactionId!, transaction.PlaidTransactionId)
            .Set(t => t.Amount, transaction.Amount)
            .Set(t => t.IsoCurrencyCode!, transaction.IsoCurrencyCode)
            .Set(t => t.Date, transaction.Date)
            .Set(t => t.AuthorizedDate!, transaction.AuthorizedDate)
            .Set(t => t.AuthorizedDatetime!, transaction.AuthorizedDatetime)
            .Set(t => t.Name, transaction.Name)
            .Set(t => t.MerchantName!, transaction.MerchantName)
            .Set(t => t.MerchantEntityId!, transaction.MerchantEntityId)
            .Set(t => t.Pending, transaction.Pending)
            .Set(t => t.PendingTransactionId!, transaction.PendingTransactionId)
            .Set(t => t.PaymentChannel!, transaction.PaymentChannel)
            .Set(t => t.TransactionType!, transaction.TransactionType)
            .Set(t => t.PfcPrimary!, transaction.PfcPrimary)
            .Set(t => t.PfcDetailed!, transaction.PfcDetailed)
            .Set(t => t.PfcConfidenceLevel!, transaction.PfcConfidenceLevel)
            .Set(t => t.PfcVersion!, transaction.PfcVersion)
            .Set(t => t.LogoUrl!, transaction.LogoUrl)
            .Set(t => t.Website!, transaction.Website)
            .Set(t => t.Status, transaction.Status)
            .Set(t => t.RemovedAt!, transaction.RemovedAt)
            .Set(t => t.UpdatedAt, transaction.UpdatedAt)
            .Update(null, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task SetRemovedAtAsync(
        Guid profileId,
        string plaidTransactionId,
        DateTimeOffset removedAt,
        CancellationToken cancellationToken = default)
    {
        var updatedAt = DateTimeOffset.UtcNow;

        _ = await supabaseClient
            .From<Transaction>()
            .Where(t => t.ProfileId == profileId)
            .Where(t => t.PlaidTransactionId == plaidTransactionId)
            .Set(t => t.RemovedAt!, removedAt)
            .Set(t => t.UpdatedAt, updatedAt)
            .Update(null, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<int> DeleteByIdsForProfileAsync(
        Guid profileId,
        IReadOnlyList<Guid> transactionIds,
        CancellationToken cancellationToken = default)
    {
        if (transactionIds.Count == 0)
            return 0;

        const string sql =
            """
            DELETE FROM transactions
            WHERE profile_id = @profile_id
              AND id = ANY(@transaction_ids)
            """;

        await using var conn = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var cmd = new NpgsqlCommand(sql, conn);

        cmd.Parameters.AddWithValue("profile_id", profileId);
        cmd.Parameters.Add(new NpgsqlParameter("transaction_ids", NpgsqlDbType.Array | NpgsqlDbType.Uuid)
        {
            Value = transactionIds.ToArray()
        });

        return await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<int> DeleteByProfileAndLinkedBankIdAsync(
        Guid profileId,
        Guid linkedBankId,
        CancellationToken cancellationToken = default)
    {
        const string sql =
            """
            DELETE FROM transactions t
            WHERE t.profile_id = @profile_id
              AND EXISTS (
                  SELECT 1
                  FROM linked_bank_accounts a
                  INNER JOIN linked_banks b ON b.id = a.linked_bank_id
                  WHERE a.id = t.linked_bank_account_id
                    AND a.linked_bank_id = @linked_bank_id
                    AND b.profile_id = @profile_id
              )
            """;

        await using var conn = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var cmd = new NpgsqlCommand(sql, conn);

        cmd.Parameters.AddWithValue("profile_id", profileId);
        cmd.Parameters.AddWithValue("linked_bank_id", linkedBankId);

        return await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<int> SetStatusByLinkedBankAccountIdAsync(
        Guid profileId,
        Guid linkedBankAccountId,
        string status,
        CancellationToken cancellationToken = default)
    {
        var updatedAt = DateTimeOffset.UtcNow;

        const string sql =
            """
            UPDATE transactions
            SET status = @status,
                updated_at = @updated_at
            WHERE profile_id = @profile_id
              AND linked_bank_account_id = @linked_bank_account_id
              AND removed_at IS NULL
            """;

        await using var conn = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var cmd = new NpgsqlCommand(sql, conn);

        cmd.Parameters.AddWithValue("profile_id", profileId);
        cmd.Parameters.AddWithValue("linked_bank_account_id", linkedBankAccountId);
        cmd.Parameters.AddWithValue("status", status);
        cmd.Parameters.AddWithValue("updated_at", updatedAt);

        return await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<int> DeleteByProfileAndLinkedBankAccountIdAsync(
        Guid profileId,
        Guid linkedBankAccountId,
        CancellationToken cancellationToken = default)
    {
        const string sql =
            """
            DELETE FROM transactions
            WHERE profile_id = @profile_id
              AND linked_bank_account_id = @linked_bank_account_id
            """;

        await using var conn = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var cmd = new NpgsqlCommand(sql, conn);

        cmd.Parameters.AddWithValue("profile_id", profileId);
        cmd.Parameters.AddWithValue("linked_bank_account_id", linkedBankAccountId);

        return await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<long> CountAsync(
        Guid profileId,
        TransactionQueryFilters query,
        CancellationToken cancellationToken = default)
    {
        ValidateFilters(query);

        var sql = new StringBuilder(
            """
            SELECT COUNT(*)::bigint
            FROM transactions
            WHERE profile_id = @profile_id
              AND removed_at IS NULL
            """);

        await using var conn = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var cmd = new NpgsqlCommand(null, conn);

        cmd.Parameters.AddWithValue("profile_id", profileId);
        AppendFilters(cmd, sql, query);

        cmd.CommandText = sql.ToString();

        var scalar = await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);

        return scalar is long count
            ? count
            : Convert.ToInt64(scalar, CultureInfo.InvariantCulture);
    }

    public async Task<IReadOnlyList<Transaction>> QueryPagedAsync(
        Guid profileId,
        TransactionQueryFilters query,
        CancellationToken cancellationToken = default)
    {
        ValidatePagedQuery(query);

        var sql = new StringBuilder(
            """
            SELECT
                id,
                profile_id,
                linked_bank_account_id,
                plaid_transaction_id,
                amount,
                iso_currency_code,
                date,
                authorized_date,
                authorized_datetime,
                name,
                merchant_name,
                merchant_entity_id,
                pending,
                pending_transaction_id,
                payment_channel,
                transaction_type,
                pfc_primary,
                pfc_detailed,
                pfc_confidence_level,
                pfc_version,
                logo_url,
                website,
                status,
                removed_at,
                created_at,
                updated_at
            FROM transactions
            WHERE profile_id = @profile_id
              AND removed_at IS NULL
            """);

        await using var conn = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var cmd = new NpgsqlCommand(null, conn);

        cmd.Parameters.AddWithValue("profile_id", profileId);
        AppendFilters(cmd, sql, query);

        sql.AppendLine();
        sql.Append("ORDER BY ");
        sql.Append(BuildOrderByClause(query.SortBy, query.Descending));
        sql.AppendLine();
        sql.Append("OFFSET @offset");
        sql.AppendLine();
        sql.Append("LIMIT @limit;");

        cmd.Parameters.AddWithValue("offset", query.Offset);
        cmd.Parameters.AddWithValue("limit", query.Limit);
        cmd.CommandText = sql.ToString();

        var transactions = new List<Transaction>();

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            transactions.Add(MapTransaction(reader));
        }

        return transactions;
    }

    public async Task<IReadOnlyList<Transaction>> ListRecentForProfileAsync(
        Guid profileId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        if (limit < 1)
            throw new ArgumentOutOfRangeException(nameof(limit), "limit must be at least 1.");

        const string sql =
            """
            SELECT
                id,
                profile_id,
                linked_bank_account_id,
                plaid_transaction_id,
                amount,
                iso_currency_code,
                date,
                authorized_date,
                authorized_datetime,
                name,
                merchant_name,
                merchant_entity_id,
                pending,
                pending_transaction_id,
                payment_channel,
                transaction_type,
                pfc_primary,
                pfc_detailed,
                pfc_confidence_level,
                pfc_version,
                logo_url,
                website,
                status,
                removed_at,
                created_at,
                updated_at
            FROM transactions
            WHERE profile_id = @profile_id
              AND removed_at IS NULL
              AND status IN ('active', 'account_opted_out')
            ORDER BY date DESC, id DESC
            LIMIT @limit;
            """;

        await using var conn = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("profile_id", profileId);
        cmd.Parameters.AddWithValue("limit", limit);

        var transactions = new List<Transaction>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            transactions.Add(MapTransaction(reader));

        return transactions;
    }

    public async Task<(decimal TotalIncome, decimal TotalExpenses)> SumIncomeAndExpenseAsync(
        Guid profileId,
        TransactionQueryFilters query,
        CancellationToken cancellationToken = default)
    {
        ValidateFilters(query);

        var sql = new StringBuilder(
            """
            SELECT
                COALESCE(SUM(CASE WHEN amount < 0 THEN -amount ELSE 0 END), 0)::numeric,
                COALESCE(SUM(CASE WHEN amount > 0 THEN amount ELSE 0 END), 0)::numeric
            FROM transactions
            WHERE profile_id = @profile_id
              AND removed_at IS NULL
            """);

        await using var conn = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var cmd = new NpgsqlCommand(null, conn);

        cmd.Parameters.AddWithValue("profile_id", profileId);
        AppendFilters(cmd, sql, query);

        cmd.CommandText = sql.ToString();

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            return (0m, 0m);

        var income = reader.GetDecimal(0);
        var expenses = reader.GetDecimal(1);
        return (income, expenses);
    }

    public async Task<IReadOnlyList<(string? PfcPrimary, decimal TotalExpenses)>> SumExpensesByPfcPrimaryAsync(
        Guid profileId,
        TransactionQueryFilters query,
        CancellationToken cancellationToken = default)
    {
        ValidateFilters(query);

        var sql = new StringBuilder(
            """
            SELECT s.pfc_primary, s.total_expenses
            FROM (
                SELECT
                    pfc_primary,
                    COALESCE(SUM(CASE WHEN amount > 0 THEN amount ELSE 0 END), 0)::numeric AS total_expenses
                FROM transactions
                WHERE profile_id = @profile_id
                  AND removed_at IS NULL
            """);

        await using var conn = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var cmd = new NpgsqlCommand(null, conn);

        cmd.Parameters.AddWithValue("profile_id", profileId);
        AppendFilters(cmd, sql, query);

        sql.Append(
            """
             GROUP BY pfc_primary
            ) AS s
            WHERE s.total_expenses <> 0
            ORDER BY s.total_expenses DESC;
            """);

        cmd.CommandText = sql.ToString();

        var rows = new List<(string?, decimal)>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var pfc = reader.IsDBNull(0) ? null : reader.GetString(0);
            var total = reader.GetDecimal(1);
            rows.Add((pfc, total));
        }

        return rows;
    }

    public async Task<IReadOnlyList<(DateOnly PeriodStartDate, string? PfcPrimary, decimal ExpenseTotal)>>
        GetStackedExpensesByPfcPrimarySeriesAsync(
            Guid profileId,
            TransactionQueryFilters query,
            string timeGranularity,
            CancellationToken cancellationToken = default)
    {
        ValidateFilters(query);

        var dateTruncUnit = ToDateTruncUnitLiteral(timeGranularity);

        var sql = new StringBuilder(
            $"""
            SELECT s.period_start_date, s.pfc_primary, s.total_expenses
            FROM (
                SELECT
                    (date_trunc('{dateTruncUnit}', transactions.date::timestamp))::date AS period_start_date,
                    pfc_primary,
                    COALESCE(SUM(CASE WHEN amount > 0 THEN amount ELSE 0 END), 0)::numeric AS total_expenses
                FROM transactions
                WHERE profile_id = @profile_id
                  AND removed_at IS NULL
            """);

        await using var conn = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var cmd = new NpgsqlCommand(null, conn);

        cmd.Parameters.AddWithValue("profile_id", profileId);
        AppendFilters(cmd, sql, query);

        sql.Append(
            $"""
                GROUP BY (date_trunc('{dateTruncUnit}', transactions.date::timestamp))::date, pfc_primary
            ) AS s
            WHERE s.total_expenses <> 0
            ORDER BY s.period_start_date, s.pfc_primary;
            """);

        cmd.CommandText = sql.ToString();

        var rows = new List<(DateOnly, string?, decimal)>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var periodStartDate = reader.GetFieldValue<DateOnly>(0);
            var pfcPrimary = reader.IsDBNull(1) ? null : reader.GetString(1);
            var expenseTotal = reader.GetDecimal(2);
            rows.Add((periodStartDate, pfcPrimary, expenseTotal));
        }

        return rows;
    }

    public async Task<IReadOnlyList<(DateOnly PeriodStartDate, Guid? LinkedBankAccountId, string? OfficialName, decimal ExpenseTotal)>>
        GetGroupedExpensesByAccountSeriesAsync(
            Guid profileId,
            TransactionQueryFilters query,
            string timeGranularity,
            CancellationToken cancellationToken = default)
    {
        ValidateFilters(query);

        var dateTruncUnit = ToDateTruncUnitLiteral(timeGranularity);

        var sql = new StringBuilder(
            $"""
            SELECT s.period_start_date, s.linked_bank_account_id, s.official_name, s.total_expenses
            FROM (
                SELECT
                    (date_trunc('{dateTruncUnit}', transactions.date::timestamp))::date AS period_start_date,
                    transactions.linked_bank_account_id AS linked_bank_account_id,
                    MAX(NULLIF(TRIM(lba.official_name), '')) AS official_name,
                    COALESCE(SUM(CASE WHEN transactions.amount > 0 THEN transactions.amount ELSE 0 END), 0)::numeric AS total_expenses
                FROM transactions
                LEFT JOIN linked_bank_accounts lba ON lba.id = transactions.linked_bank_account_id
                WHERE transactions.profile_id = @profile_id
                  AND transactions.removed_at IS NULL
            """);

        await using var conn = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var cmd = new NpgsqlCommand(null, conn);

        cmd.Parameters.AddWithValue("profile_id", profileId);
        AppendFilters(cmd, sql, query);

        sql.Append(
            $"""
                GROUP BY (date_trunc('{dateTruncUnit}', transactions.date::timestamp))::date, transactions.linked_bank_account_id
            ) AS s
            WHERE s.total_expenses <> 0
            ORDER BY s.period_start_date, s.linked_bank_account_id;
            """);

        cmd.CommandText = sql.ToString();

        var rows = new List<(DateOnly, Guid?, string?, decimal)>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var periodStartDate = reader.GetFieldValue<DateOnly>(0);
            var accountId = reader.IsDBNull(1) ? (Guid?)null : reader.GetGuid(1);
            var officialName = reader.IsDBNull(2) ? null : reader.GetString(2);
            var expenseTotal = reader.GetDecimal(3);
            rows.Add((periodStartDate, accountId, officialName, expenseTotal));
        }

        return rows;
    }

    private async Task<NpgsqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connectionString = configuration.GetConnectionString("Default");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:Default is required for raw transaction queries.");
        }

        var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(cancellationToken).ConfigureAwait(false);
        return conn;
    }

    private static void ValidatePagedQuery(TransactionQueryFilters query)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.Offset < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(query.Offset), "Offset must be >= 0.");
        }

        if (query.Limit <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(query.Limit), "Limit must be > 0.");
        }

        ValidateFilters(query);
    }

    private static void ValidateFilters(TransactionQueryFilters query)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.DateFromInclusive is { } from &&
            query.DateToInclusive is { } to &&
            from > to)
        {
            throw new ArgumentException("DateFromInclusive cannot be greater than DateToInclusive.");
        }

        if (query.AbsAmountMin is < 0)
        {
            throw new ArgumentException("AbsAmountMin must be non-negative.");
        }

        if (query.AbsAmountMax is < 0)
        {
            throw new ArgumentException("AbsAmountMax must be non-negative.");
        }

        if (query.AbsAmountMin is { } min &&
            query.AbsAmountMax is { } max &&
            min > max)
        {
            throw new ArgumentException("AbsAmountMin cannot be greater than AbsAmountMax.");
        }
    }

    private static string ToDateTruncUnitLiteral(string timeGranularity) =>
        timeGranularity switch
        {
            "day" => "day",
            "week" => "week",
            "month" => "month",
            "year" => "year",
            _ => throw new ArgumentOutOfRangeException(nameof(timeGranularity), timeGranularity, null)
        };

    private static void AppendFilters(
        NpgsqlCommand cmd,
        StringBuilder sql,
        TransactionQueryFilters filters)
    {
        AppendAccountFilter(cmd, sql, filters);
        AppendPfcPrimaryFilter(cmd, sql, filters);
        AppendPaymentChannelFilter(cmd, sql, filters);
        AppendPendingFilter(cmd, sql, filters);
        AppendDateRangeFilter(cmd, sql, filters);
        AppendAmountFilter(cmd, sql, filters);
        AppendAmountFlowFilter(sql, filters);
    }

    private static void AppendAccountFilter(
        NpgsqlCommand cmd,
        StringBuilder sql,
        TransactionQueryFilters filters)
    {
        var accountIds = filters.AccountIds
            .Distinct()
            .ToArray();

        if (!filters.IncludeUnlinkedTransactions)
        {
            if (accountIds.Length == 0)
            {
                sql.Append(" AND FALSE");
                return;
            }

            sql.Append(" AND linked_bank_account_id IS NOT NULL");
            sql.Append(" AND status <> 'account_opted_out'");
            sql.Append(" AND linked_bank_account_id = ANY(@filter_account_ids)");

            AddUuidArrayParameter(cmd, "filter_account_ids", accountIds);
            return;
        }

        if (accountIds.Length == 0)
        {
            sql.Append(" AND (linked_bank_account_id IS NULL OR status = 'account_opted_out')");
            return;
        }

        sql.Append(
            """
             AND (
                linked_bank_account_id IS NULL
                OR status = 'account_opted_out'
                OR linked_bank_account_id = ANY(@filter_account_ids)
            )
            """);

        AddUuidArrayParameter(cmd, "filter_account_ids", accountIds);
    }

    private static void AppendPfcPrimaryFilter(
        NpgsqlCommand cmd,
        StringBuilder sql,
        TransactionQueryFilters filters)
    {
        var hasPfcPrimaryList = filters.PfcPrimaryList.Count > 0;
        var includeUncategorized = filters.IncludePfcUncategorized;

        if (!hasPfcPrimaryList && !includeUncategorized)
        {
            sql.Append(" AND FALSE");
            return;
        }

        sql.Append(" AND (");

        if (hasPfcPrimaryList)
        {
            sql.Append("lower(coalesce(pfc_primary, '')) = ANY(@filter_pfc_primary_list)");

            var normalizedPfcCodes = filters.PfcPrimaryList
                .Distinct(StringComparer.Ordinal)
                .Select(value => value.ToLowerInvariant())
                .ToArray();

            AddTextArrayParameter(cmd, "filter_pfc_primary_list", normalizedPfcCodes);
        }

        if (hasPfcPrimaryList && includeUncategorized)
        {
            sql.Append(" OR ");
        }

        if (includeUncategorized)
        {
            sql.Append("pfc_primary IS NULL");
        }

        sql.Append(')');
    }

    private static void AppendPaymentChannelFilter(
        NpgsqlCommand cmd,
        StringBuilder sql,
        TransactionQueryFilters filters)
    {
        if (filters.PaymentChannels.Count == 0)
        {
            sql.Append(" AND FALSE");
            return;
        }

        sql.Append(" AND lower(coalesce(payment_channel, '')) = ANY(@filter_payment_channels)");

        var normalizedChannels = filters.PaymentChannels
            .Distinct(StringComparer.Ordinal)
            .Select(value => value.ToLowerInvariant())
            .ToArray();

        AddTextArrayParameter(cmd, "filter_payment_channels", normalizedChannels);
    }

    private static void AppendPendingFilter(
        NpgsqlCommand cmd,
        StringBuilder sql,
        TransactionQueryFilters filters)
    {
        if (filters.Pending is not { } pending)
        {
            return;
        }

        sql.Append(" AND pending = @filter_pending");
        cmd.Parameters.AddWithValue("filter_pending", pending);
    }

    private static void AppendDateRangeFilter(
        NpgsqlCommand cmd,
        StringBuilder sql,
        TransactionQueryFilters filters)
    {
        if (filters.DateFromInclusive is { } dateFrom)
        {
            sql.Append(" AND date >= @filter_date_from");
            cmd.Parameters.Add(new NpgsqlParameter("filter_date_from", NpgsqlDbType.Date)
            {
                Value = dateFrom
            });
        }

        if (filters.DateToInclusive is { } dateTo)
        {
            sql.Append(" AND date <= @filter_date_to");
            cmd.Parameters.Add(new NpgsqlParameter("filter_date_to", NpgsqlDbType.Date)
            {
                Value = dateTo
            });
        }
    }

    private static void AppendAmountFilter(
        NpgsqlCommand cmd,
        StringBuilder sql,
        TransactionQueryFilters filters)
    {
        if (filters.AbsAmountMin is { } minAmount)
        {
            sql.Append(" AND abs(amount) >= @filter_abs_amount_min");
            cmd.Parameters.AddWithValue("filter_abs_amount_min", minAmount);
        }

        if (filters.AbsAmountMax is { } maxAmount)
        {
            sql.Append(" AND abs(amount) <= @filter_abs_amount_max");
            cmd.Parameters.AddWithValue("filter_abs_amount_max", maxAmount);
        }
    }

    private static void AppendAmountFlowFilter(
        StringBuilder sql,
        TransactionQueryFilters filters)
    {
        if (filters.AmountFlow is not { } flow)
        {
            return;
        }

        sql.Append(
            flow == TransactionFlow.Income
                ? " AND amount < 0"
                : " AND amount > 0");
    }

    private static void AddUuidArrayParameter(
        NpgsqlCommand cmd,
        string name,
        Guid[] values)
    {
        cmd.Parameters.Add(new NpgsqlParameter(name, NpgsqlDbType.Array | NpgsqlDbType.Uuid)
        {
            Value = values
        });
    }

    private static void AddTextArrayParameter(
        NpgsqlCommand cmd,
        string name,
        string[] values)
    {
        cmd.Parameters.Add(new NpgsqlParameter(name, NpgsqlDbType.Array | NpgsqlDbType.Text)
        {
            Value = values
        });
    }

    private static string BuildOrderByClause(
        TransactionSortField sortBy,
        bool descending)
    {
        var direction = descending ? "DESC" : "ASC";
        const string NullsLast = "NULLS LAST";

        var primaryOrder = sortBy switch
        {
            TransactionSortField.Date => $"date {direction}",
            TransactionSortField.MerchantName => $"COALESCE(merchant_name, name) {direction} {NullsLast}",
            TransactionSortField.LinkedBankAccountId => $"linked_bank_account_id {direction} {NullsLast}",
            TransactionSortField.PfcPrimary => $"pfc_primary {direction} {NullsLast}",
            TransactionSortField.PfcDetailed => $"pfc_detailed {direction} {NullsLast}",
            TransactionSortField.Amount => $"amount {direction}",
            TransactionSortField.PaymentChannel => $"payment_channel {direction} {NullsLast}",
            TransactionSortField.Pending => $"pending {direction}",
            _ => throw new ArgumentOutOfRangeException(nameof(sortBy), sortBy, null)
        };

        return $"{primaryOrder}, id {direction}";
    }

    private static Transaction MapTransaction(NpgsqlDataReader reader) => new()
    {
        Id = reader.GetGuid(0),
        ProfileId = reader.GetGuid(1),
        LinkedBankAccountId = reader.IsDBNull(2) ? null : reader.GetGuid(2),
        PlaidTransactionId = reader.IsDBNull(3) ? null : reader.GetString(3),
        Amount = reader.GetDecimal(4),
        IsoCurrencyCode = reader.IsDBNull(5) ? null : reader.GetString(5),
        Date = reader.GetFieldValue<DateOnly>(6),
        AuthorizedDate = reader.IsDBNull(7) ? null : reader.GetFieldValue<DateOnly>(7),
        AuthorizedDatetime = reader.IsDBNull(8) ? null : reader.GetFieldValue<DateTimeOffset>(8),
        Name = reader.GetString(9),
        MerchantName = reader.IsDBNull(10) ? null : reader.GetString(10),
        MerchantEntityId = reader.IsDBNull(11) ? null : reader.GetString(11),
        Pending = reader.GetBoolean(12),
        PendingTransactionId = reader.IsDBNull(13) ? null : reader.GetString(13),
        PaymentChannel = reader.IsDBNull(14) ? null : reader.GetString(14),
        TransactionType = reader.IsDBNull(15) ? null : reader.GetString(15),
        PfcPrimary = reader.IsDBNull(16) ? null : reader.GetString(16),
        PfcDetailed = reader.IsDBNull(17) ? null : reader.GetString(17),
        PfcConfidenceLevel = reader.IsDBNull(18) ? null : reader.GetString(18),
        PfcVersion = reader.IsDBNull(19) ? null : reader.GetString(19),
        LogoUrl = reader.IsDBNull(20) ? null : reader.GetString(20),
        Website = reader.IsDBNull(21) ? null : reader.GetString(21),
        Status = reader.GetString(22),
        RemovedAt = reader.IsDBNull(23) ? null : reader.GetFieldValue<DateTimeOffset>(23),
        CreatedAt = reader.GetFieldValue<DateTimeOffset>(24),
        UpdatedAt = reader.GetFieldValue<DateTimeOffset>(25)
    };
}