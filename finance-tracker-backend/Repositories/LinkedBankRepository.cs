using finance_tracker_backend.Models;

namespace finance_tracker_backend.Repositories;

public sealed class LinkedBankRepository(Supabase.Client supabaseClient) : ILinkedBankRepository
{
    public async Task<LinkedBank?> GetByIdForProfileAsync(Guid id, Guid profileId, CancellationToken cancellationToken = default)
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

    public async Task<IReadOnlyList<LinkedBank>> ListByProfileIdAsync(Guid profileId, CancellationToken cancellationToken = default)
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
        await supabaseClient.From<LinkedBank>().Insert(bank, cancellationToken: cancellationToken).ConfigureAwait(false);
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

    public async Task HardDeleteByIdForProfileAsync(Guid id, Guid profileId, CancellationToken cancellationToken = default)
    {
        await supabaseClient.From<LinkedBank>()
            .Where(b => b.Id == id)
            .Where(b => b.ProfileId == profileId)
            .Delete(null, cancellationToken)
            .ConfigureAwait(false);
    }
}
