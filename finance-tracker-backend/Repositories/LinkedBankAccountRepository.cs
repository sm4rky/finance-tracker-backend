using finance_tracker_backend.Models;

namespace finance_tracker_backend.Repositories;

public sealed class LinkedBankAccountRepository(Supabase.Client supabaseClient) : ILinkedBankAccountRepository
{
    public async Task<LinkedBankAccount?> GetByLinkedBankAndPlaidAccountIdAsync(
        Guid linkedBankId,
        string plaidAccountId,
        CancellationToken cancellationToken = default)
    {
        var result = await supabaseClient.From<LinkedBankAccount>()
            .Where(a => a.LinkedBankId == linkedBankId)
            .Where(a => a.PlaidAccountId == plaidAccountId)
            .Get(cancellationToken)
            .ConfigureAwait(false);
        return result.Models.Count > 0 ? result.Models[0] : null;
    }

    public async Task<IReadOnlyList<LinkedBankAccount>> ListByLinkedBankIdAsync(
        Guid linkedBankId,
        CancellationToken cancellationToken = default)
    {
        var result = await supabaseClient.From<LinkedBankAccount>()
            .Where(a => a.LinkedBankId == linkedBankId)
            .Get(cancellationToken)
            .ConfigureAwait(false);
        return result.Models;
    }

    public async Task<IReadOnlyList<LinkedBankAccount>> ListByLinkedBankIdsAsync(
        IReadOnlyCollection<Guid> linkedBankIds,
        CancellationToken cancellationToken = default)
    {
        if (linkedBankIds.Count == 0)
            return [];

        var list = new List<LinkedBankAccount>();
        foreach (var id in linkedBankIds)
        {
            var chunk = await supabaseClient.From<LinkedBankAccount>()
                .Where(a => a.LinkedBankId == id)
                .Get(cancellationToken)
                .ConfigureAwait(false);
            list.AddRange(chunk.Models);
        }

        return list;
    }

    public async Task InsertAsync(LinkedBankAccount account, CancellationToken cancellationToken = default)
    {
        await supabaseClient.From<LinkedBankAccount>().Insert(account, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task UpdateAsync(LinkedBankAccount account, CancellationToken cancellationToken = default)
    {
        await supabaseClient.From<LinkedBankAccount>()
            .Where(a => a.Id == account.Id)
            .Set(a => a.PlaidAccountId, account.PlaidAccountId)
            .Set(a => a.AccountName ?? string.Empty, account.AccountName)
            .Set(a => a.OfficialName ?? string.Empty, account.OfficialName)
            .Set(a => a.Mask ?? string.Empty, account.Mask)
            .Set(a => a.Type ?? string.Empty, account.Type)
            .Set(a => a.Subtype ?? string.Empty, account.Subtype)
            .Set(a => a.CurrentBalance ?? 0, account.CurrentBalance)
            .Set(a => a.AvailableBalance ?? 0, account.AvailableBalance)
            .Set(a => a.LimitAmount ?? 0, account.LimitAmount)
            .Set(a => a.IsoCurrencyCode ?? string.Empty, account.IsoCurrencyCode)
            .Set(a => a.UnofficialCurrencyCode ?? string.Empty, account.UnofficialCurrencyCode)
            .Set(a => a.BalanceLastFetchedAt!, account.BalanceLastFetchedAt)
            .Set(a => a.IsActive, account.IsActive)
            .Set(a => a.UpdatedAt, account.UpdatedAt)
            .Update(null, cancellationToken)
            .ConfigureAwait(false);
    }
}
