using finance_tracker_backend.Models;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace finance_tracker_backend.Repositories;

public sealed class LinkedBankRepository(Supabase.Client supabaseClient, IConfiguration configuration)
    : ILinkedBankRepository
{
    public async Task<LinkedBank?> GetByIdForProfileAsync(Guid id, Guid profileId,
        CancellationToken cancellationToken = default)
    {
        var result = await supabaseClient.From<LinkedBank>()
            .Where(b => b.Id == id)
            .Where(b => b.ProfileId == profileId)
            .Get(cancellationToken)
            .ConfigureAwait(false);
        return result.Models.Count > 0 ? result.Models[0] : null;
    }

    public async Task<LinkedBank?> GetActiveByPlaidItemIdAsync(
        Guid profileId,
        string plaidItemId,
        CancellationToken cancellationToken = default)
    {
        var result = await supabaseClient.From<LinkedBank>()
            .Where(b => b.ProfileId == profileId)
            .Where(b => b.PlaidItemId == plaidItemId)
            .Get(cancellationToken)
            .ConfigureAwait(false);
        return result.Models.FirstOrDefault(b => b.Status != "soft_deleted");
    }

    public async Task<IReadOnlyList<LinkedBank>> ListByProfileIdAsync(Guid profileId,
        CancellationToken cancellationToken = default)
    {
        var result = await supabaseClient.From<LinkedBank>()
            .Where(b => b.ProfileId == profileId)
            .Where(b => b.Status != "soft_deleted")
            .Get(cancellationToken)
            .ConfigureAwait(false);
        return result.Models;
    }

    public async Task InsertAsync(LinkedBank bank, CancellationToken cancellationToken = default)
    {
        await supabaseClient.From<LinkedBank>().Insert(bank, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task UpdateAsync(LinkedBank bank, CancellationToken cancellationToken = default)
    {
        await supabaseClient.From<LinkedBank>()
            .Where(b => b.Id == bank.Id)
            .Where(b => b.ProfileId == bank.ProfileId)
            .Set(b => b.PlaidItemId, bank.PlaidItemId)
            .Set(b => b.PlaidAccessTokenEncrypted!, bank.PlaidAccessTokenEncrypted)
            .Set(b => b.InstitutionId!, bank.InstitutionId)
            .Set(b => b.InstitutionName!, bank.InstitutionName)
            .Set(b => b.Status, bank.Status)
            .Set(b => b.TokenRemovedAt!, bank.TokenRemovedAt)
            .Set(b => b.DisconnectedAt!, bank.DisconnectedAt)
            .Set(b => b.LastSyncedAt!, bank.LastSyncedAt)
            .Set(b => b.PlaidTransactionsCursor!, bank.PlaidTransactionsCursor)
            .Set(b => b.PendingDeselectedPlaidAccountIds!, bank.PendingDeselectedPlaidAccountIds)
            .Set(b => b.UpdatedAt, bank.UpdatedAt)
            .Update(null, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task HardDeleteByIdForProfileAsync(Guid id, Guid profileId,
        CancellationToken cancellationToken = default)
    {
        await supabaseClient.From<LinkedBank>()
            .Where(b => b.Id == id)
            .Where(b => b.ProfileId == profileId)
            .Delete(null, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<(Guid ProfileId, Guid LinkedBankId)>> ListActiveLinkedBanksAfterIdAsync(
        Guid afterBankId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        if (limit < 1)
            throw new ArgumentOutOfRangeException(nameof(limit), limit, "limit must be at least 1.");

        const string sql =
            """
            SELECT profile_id, id
            FROM linked_banks
            WHERE status = 'active'
              AND plaid_access_token_encrypted IS NOT NULL
              AND length(trim(plaid_access_token_encrypted)) > 0
              AND id > @after_id
            ORDER BY id ASC
            LIMIT @limit
            """;

        await using var conn = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("after_id", afterBankId);
        cmd.Parameters.AddWithValue("limit", limit);

        var list = new List<(Guid, Guid)>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            list.Add((reader.GetGuid(0), reader.GetGuid(1)));
        }

        return list;
    }

    private async Task<NpgsqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connectionString = configuration.GetConnectionString("Default");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:Default is required for linked bank batch queries.");
        }

        var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(cancellationToken).ConfigureAwait(false);
        return conn;
    }
}