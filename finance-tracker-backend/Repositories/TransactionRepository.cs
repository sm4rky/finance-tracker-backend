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
    public async Task<Transaction?> GetByProfileAndPlaidTransactionIdAsync(
        Guid profileId,
        string plaidTransactionId,
        CancellationToken cancellationToken = default)
    {
        var result = await supabaseClient.From<Transaction>()
            .Where(t => t.ProfileId == profileId)
            .Where(t => t.PlaidTransactionId == plaidTransactionId)
            .Get(cancellationToken)
            .ConfigureAwait(false);
        return result.Models.Count > 0 ? result.Models[0] : null;
    }

    public async Task InsertAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        await supabaseClient.From<Transaction>().Insert(transaction, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task UpdateAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        await supabaseClient.From<Transaction>()
            .Where(t => t.Id == transaction.Id)
            .Where(t => t.ProfileId == transaction.ProfileId)
            .Set(t => t.LinkedBankAccountId!, transaction.LinkedBankAccountId)
            .Set(t => t.PlaidTransactionId, transaction.PlaidTransactionId)
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
        var now = DateTimeOffset.UtcNow;
        _ = await supabaseClient.From<Transaction>()
            .Where(t => t.ProfileId == profileId)
            .Where(t => t.PlaidTransactionId == plaidTransactionId)
            .Set(t => t.RemovedAt!, removedAt)
            .Set(t => t.UpdatedAt, now)
            .Update(null, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<int> DeleteByProfileAndLinkedBankIdAsync(
        Guid profileId,
        Guid linkedBankId,
        CancellationToken cancellationToken = default)
    {
        var cs = GetRequiredConnectionString();
        await using var conn = new NpgsqlConnection(cs);
        await conn.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var cmd = new NpgsqlCommand(
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
            """,
            conn);
        cmd.Parameters.AddWithValue("profile_id", profileId);
        cmd.Parameters.AddWithValue("linked_bank_id", linkedBankId);
        var n = await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        return n;
    }

    public async Task<long> CountAsync(Guid profileId, TransactionQueryFilters query, CancellationToken cancellationToken = default)
    {
        ValidateFilterFields(query);

        var cs = GetRequiredConnectionString();

        var where = new StringBuilder(
            """
            WHERE profile_id = @profile_id
              AND removed_at IS NULL
            """);

        await using var conn = new NpgsqlConnection(cs);
        await conn.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var cmd = new NpgsqlCommand(null, conn);
        cmd.Parameters.AddWithValue("profile_id", profileId);

        AppendTransactionFilters(cmd, where, query);

        cmd.CommandText = $"""
                           SELECT COUNT(*)::bigint
                           FROM transactions
                           {where}
                           """;

        var scalar = await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return scalar is long l
            ? l
            : Convert.ToInt64(scalar, CultureInfo.InvariantCulture);
    }

    public async Task<IReadOnlyList<Transaction>> QueryPagedAsync(
        Guid profileId,
        TransactionQueryFilters query,
        CancellationToken cancellationToken = default)
    {
        ValidatePagedQuery(query);

        var cs = GetRequiredConnectionString();
        var orderBy = BuildOrderByClause(query.SortBy, query.Descending);

        var where = new StringBuilder(
            """
            WHERE profile_id = @profile_id
              AND removed_at IS NULL
            """);

        await using var conn = new NpgsqlConnection(cs);
        await conn.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var cmd = new NpgsqlCommand(null, conn);
        cmd.Parameters.AddWithValue("profile_id", profileId);

        AppendTransactionFilters(cmd, where, query);
        cmd.Parameters.AddWithValue("offset", query.Offset);
        cmd.Parameters.AddWithValue("limit", query.Limit);

        cmd.CommandText = $"""
                           SELECT id, profile_id, linked_bank_account_id, plaid_transaction_id, amount, iso_currency_code,
                                  date, authorized_date, authorized_datetime, name, merchant_name, merchant_entity_id,
                                  pending, pending_transaction_id, payment_channel, transaction_type,
                                  pfc_primary, pfc_detailed, pfc_confidence_level, pfc_version,
                                  logo_url, website, status, removed_at, created_at, updated_at
                           FROM transactions
                           {where}
                           ORDER BY {orderBy}
                           OFFSET @offset
                           LIMIT @limit;
                           """;

        var list = new List<Transaction>();

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            list.Add(ReadTransactionRow(reader));

        return list;
    }

    private string GetRequiredConnectionString()
    {
        var cs = configuration.GetConnectionString("Default");
        if (string.IsNullOrWhiteSpace(cs))
            throw new InvalidOperationException(
                "ConnectionStrings:Default is required for transaction list pagination.");

        return cs;
    }

    private static void ValidatePagedQuery(TransactionQueryFilters query)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.Offset < 0)
            throw new ArgumentOutOfRangeException(nameof(query.Offset), "Offset must be >= 0.");

        if (query.Limit <= 0)
            throw new ArgumentOutOfRangeException(nameof(query.Limit), "Limit must be > 0.");

        ValidateFilterFields(query);
    }

    private static void ValidateFilterFields(TransactionQueryFilters query)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.DateFromInclusive is { } from &&
            query.DateToInclusive is { } to &&
            from > to)
        {
            throw new ArgumentException("DateFromInclusive cannot be greater than DateToInclusive.");
        }

        if (query.AbsAmountMin is < 0)
            throw new ArgumentException("AbsAmountMin must be non-negative.");

        if (query.AbsAmountMax is < 0)
            throw new ArgumentException("AbsAmountMax must be non-negative.");

        if (query.AbsAmountMin is { } min &&
            query.AbsAmountMax is { } max &&
            min > max)
        {
            throw new ArgumentException("AbsAmountMin cannot be greater than AbsAmountMax.");
        }
    }

    private static void AppendTransactionFilters(
        NpgsqlCommand cmd,
        StringBuilder where,
        TransactionQueryFilters filters)
    {
        if (filters.AccountIds.Count > 0)
        {
            where.Append(" AND linked_bank_account_id = ANY(@filter_account_ids)");
            cmd.Parameters.Add(new NpgsqlParameter("filter_account_ids", NpgsqlDbType.Array | NpgsqlDbType.Uuid)
            {
                Value = filters.AccountIds.Distinct().ToArray()
            });
        }

        var hasPfc = filters.PfcPrimaryList.Count > 0;
        var hasUncategorized = filters.IncludePfcUncategorized;

        if (hasPfc || hasUncategorized)
        {
            where.Append(" AND (");

            switch (hasPfc)
            {
                case true when hasUncategorized:
                    where.Append("lower(coalesce(pfc_primary, '')) = ANY(@filter_pfc_primary_list) OR ");
                    AppendPfcUncategorizedPredicate(where);
                    break;
                case true:
                    where.Append("lower(coalesce(pfc_primary, '')) = ANY(@filter_pfc_primary_list)");
                    break;
                default:
                    AppendPfcUncategorizedPredicate(where);
                    break;
            }

            where.Append(')');

            if (hasPfc)
            {
                var pfcLower = filters.PfcPrimaryList
                    .Distinct(StringComparer.Ordinal)
                    .Select(s => s.ToLowerInvariant())
                    .ToArray();
                cmd.Parameters.Add(
                    new NpgsqlParameter("filter_pfc_primary_list", NpgsqlDbType.Array | NpgsqlDbType.Text)
                    {
                        Value = pfcLower
                    });
            }
        }

        if (filters.PaymentChannels.Count > 0)
        {
            where.Append(" AND lower(coalesce(payment_channel, '')) = ANY(@filter_payment_channels)");
            var channelsLower = filters.PaymentChannels
                .Distinct(StringComparer.Ordinal)
                .Select(s => s.ToLowerInvariant())
                .ToArray();
            cmd.Parameters.Add(new NpgsqlParameter("filter_payment_channels", NpgsqlDbType.Array | NpgsqlDbType.Text)
            {
                Value = channelsLower
            });
        }

        if (filters.Pending is { } pending)
        {
            where.Append(" AND pending = @filter_pending");
            cmd.Parameters.AddWithValue("filter_pending", pending);
        }

        if (filters.DateFromInclusive is { } dateFrom)
        {
            where.Append(" AND date >= @filter_date_from");
            cmd.Parameters.Add(new NpgsqlParameter("filter_date_from", NpgsqlDbType.Date)
            {
                Value = dateFrom
            });
        }

        if (filters.DateToInclusive is { } dateTo)
        {
            where.Append(" AND date <= @filter_date_to");
            cmd.Parameters.Add(new NpgsqlParameter("filter_date_to", NpgsqlDbType.Date)
            {
                Value = dateTo
            });
        }

        if (filters.AbsAmountMin is { } absAmountMin)
        {
            where.Append(" AND abs(amount) >= @filter_abs_amount_min");
            cmd.Parameters.AddWithValue("filter_abs_amount_min", absAmountMin);
        }

        if (filters.AbsAmountMax is { } absAmountMax)
        {
            where.Append(" AND abs(amount) <= @filter_abs_amount_max");
            cmd.Parameters.AddWithValue("filter_abs_amount_max", absAmountMax);
        }

        if (filters.AmountFlow is { } flow)
        {
            where.Append(flow == TransactionFlow.Income
                ? " AND amount < 0"
                : " AND amount > 0");
        }
    }

    private static void AppendPfcUncategorizedPredicate(StringBuilder where)
    {
        where.Append("pfc_primary IS NULL");
    }
    
    private static string BuildOrderByClause(TransactionSortField sortBy, bool descending)
    {
        var direction = descending ? "DESC" : "ASC";
        var idDirection = descending ? "DESC" : "ASC";
        const string nullsLast = "NULLS LAST";
        var primary = sortBy switch
        {
            TransactionSortField.Date => $"date {direction}",
            TransactionSortField.MerchantName => $"COALESCE(merchant_name, name) {direction} {nullsLast}",
            TransactionSortField.LinkedBankAccountId => $"linked_bank_account_id {direction} {nullsLast}",
            TransactionSortField.PfcPrimary => $"pfc_primary {direction} {nullsLast}",
            TransactionSortField.PfcDetailed => $"pfc_detailed {direction} {nullsLast}",
            TransactionSortField.Amount => $"amount {direction}",
            TransactionSortField.PaymentChannel => $"payment_channel {direction} {nullsLast}",
            TransactionSortField.Pending => $"pending {direction}",
            _ => throw new ArgumentOutOfRangeException(nameof(sortBy), sortBy, null)
        };

        return $"{primary}, id {idDirection}";
    }

    private static Transaction ReadTransactionRow(NpgsqlDataReader reader) => new()
    {
        Id = reader.GetGuid(0),
        ProfileId = reader.GetGuid(1),
        LinkedBankAccountId = reader.IsDBNull(2) ? null : reader.GetGuid(2),
        PlaidTransactionId = reader.GetString(3),
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