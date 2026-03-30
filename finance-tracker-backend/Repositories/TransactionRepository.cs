using finance_tracker_backend.Enums;
using finance_tracker_backend.Models;
using Microsoft.Extensions.Configuration;
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

    public async Task<long> CountAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
        var cs = configuration.GetConnectionString("Default");
        if (string.IsNullOrWhiteSpace(cs))
            throw new InvalidOperationException("ConnectionStrings:Default is required for transaction list pagination.");

        const string sql = """
            SELECT COUNT(*)::bigint
            FROM transactions
            WHERE profile_id = @profile_id
              AND removed_at IS NULL;
            """;

        await using var conn = new NpgsqlConnection(cs);
        await conn.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("profile_id", profileId);
        var scalar = await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return scalar is long l ? l : Convert.ToInt64(scalar, System.Globalization.CultureInfo.InvariantCulture);
    }

    public async Task<IReadOnlyList<Transaction>> QueryPagedAsync(
        Guid profileId,
        int offset,
        int limit,
        TransactionSortByField sortBy,
        bool descending,
        CancellationToken cancellationToken = default)
    {
        var cs = configuration.GetConnectionString("Default");
        if (string.IsNullOrWhiteSpace(cs))
            throw new InvalidOperationException("ConnectionStrings:Default is required for transaction list pagination.");

        var orderBy = BuildOrderByClause(sortBy, descending);
        var sql = $"""
            SELECT id, profile_id, linked_bank_account_id, plaid_transaction_id, amount, iso_currency_code,
                   date, authorized_date, authorized_datetime, name, merchant_name, merchant_entity_id,
                   pending, pending_transaction_id, payment_channel, transaction_type,
                   pfc_primary, pfc_detailed, pfc_confidence_level, pfc_version,
                   logo_url, website, status, removed_at, created_at, updated_at
            FROM transactions
            WHERE profile_id = @profile_id
              AND removed_at IS NULL
            ORDER BY {orderBy}
            OFFSET @offset
            LIMIT @limit;
            """;

        await using var conn = new NpgsqlConnection(cs);
        await conn.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("profile_id", profileId);
        cmd.Parameters.AddWithValue("offset", offset);
        cmd.Parameters.AddWithValue("limit", limit);

        var list = new List<Transaction>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            list.Add(ReadTransactionRow(reader));

        return list;
    }

    /// <summary>Whitelist-only ORDER BY fragments; tie-break on <c>id</c>. Nullable columns use <c>NULLS LAST</c>.</summary>
    private static string BuildOrderByClause(TransactionSortByField sortBy, bool descending)
    {
        var dir = descending ? "DESC" : "ASC";
        var idDir = descending ? "DESC" : "ASC";
        const string nullsLast = "NULLS LAST";
        var primary = sortBy switch
        {
            TransactionSortByField.Date => $"date {dir}",
            TransactionSortByField.MerchantName => $"COALESCE(merchant_name, name) {dir} {nullsLast}",
            TransactionSortByField.LinkedBankAccountId => $"linked_bank_account_id {dir} {nullsLast}",
            TransactionSortByField.PfcPrimary => $"pfc_primary {dir} {nullsLast}",
            TransactionSortByField.PfcDetailed => $"pfc_detailed {dir} {nullsLast}",
            TransactionSortByField.Amount => $"amount {dir}",
            TransactionSortByField.PaymentChannel => $"payment_channel {dir} {nullsLast}",
            TransactionSortByField.Pending => $"pending {dir}",
            _ => throw new ArgumentOutOfRangeException(nameof(sortBy), sortBy, null)
        };
        return $"{primary}, id {idDir}";
    }

    private static Transaction ReadTransactionRow(NpgsqlDataReader r) => new()
    {
        Id = r.GetGuid(0),
        ProfileId = r.GetGuid(1),
        LinkedBankAccountId = r.IsDBNull(2) ? null : r.GetGuid(2),
        PlaidTransactionId = r.GetString(3),
        Amount = r.GetDecimal(4),
        IsoCurrencyCode = r.IsDBNull(5) ? null : r.GetString(5),
        Date = r.GetFieldValue<DateOnly>(6),
        AuthorizedDate = r.IsDBNull(7) ? null : r.GetFieldValue<DateOnly>(7),
        AuthorizedDatetime = r.IsDBNull(8) ? null : r.GetFieldValue<DateTimeOffset>(8),
        Name = r.GetString(9),
        MerchantName = r.IsDBNull(10) ? null : r.GetString(10),
        MerchantEntityId = r.IsDBNull(11) ? null : r.GetString(11),
        Pending = r.GetBoolean(12),
        PendingTransactionId = r.IsDBNull(13) ? null : r.GetString(13),
        PaymentChannel = r.IsDBNull(14) ? null : r.GetString(14),
        TransactionType = r.IsDBNull(15) ? null : r.GetString(15),
        PfcPrimary = r.IsDBNull(16) ? null : r.GetString(16),
        PfcDetailed = r.IsDBNull(17) ? null : r.GetString(17),
        PfcConfidenceLevel = r.IsDBNull(18) ? null : r.GetString(18),
        PfcVersion = r.IsDBNull(19) ? null : r.GetString(19),
        LogoUrl = r.IsDBNull(20) ? null : r.GetString(20),
        Website = r.IsDBNull(21) ? null : r.GetString(21),
        Status = r.GetString(22),
        RemovedAt = r.IsDBNull(23) ? null : r.GetFieldValue<DateTimeOffset>(23),
        CreatedAt = r.GetFieldValue<DateTimeOffset>(24),
        UpdatedAt = r.GetFieldValue<DateTimeOffset>(25)
    };
}
