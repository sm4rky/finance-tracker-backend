using finance_tracker_backend.Models;

namespace finance_tracker_backend.Repositories;

public sealed class ProfileBudgetBankAccountRepository(Supabase.Client supabaseClient)
    : IProfileBudgetBankAccountRepository
{
    public async Task<IReadOnlyList<ProfileBudgetBankAccount>> ListByBudgetIdAsync(
        Guid budgetId,
        CancellationToken cancellationToken = default)
    {
        var result = await supabaseClient.From<ProfileBudgetBankAccount>()
            .Where(a => a.BudgetId == budgetId)
            .Get(cancellationToken)
            .ConfigureAwait(false);

        return result.Models;
    }

    public async Task<IReadOnlyList<ProfileBudgetBankAccount>> ListByBudgetIdsAsync(
        IReadOnlyCollection<Guid> budgetIds,
        CancellationToken cancellationToken = default)
    {
        if (budgetIds.Count == 0)
            return [];

        var accounts = new List<ProfileBudgetBankAccount>();
        foreach (var budgetId in budgetIds.Distinct())
        {
            accounts.AddRange(await ListByBudgetIdAsync(budgetId, cancellationToken).ConfigureAwait(false));
        }

        return accounts;
    }

    public async Task ReplaceForBudgetAsync(
        Guid budgetId,
        IReadOnlyList<ProfileBudgetBankAccount> accounts,
        CancellationToken cancellationToken = default)
    {
        await supabaseClient.From<ProfileBudgetBankAccount>()
            .Where(a => a.BudgetId == budgetId)
            .Delete(null, cancellationToken)
            .ConfigureAwait(false);

        foreach (var account in accounts)
        {
            account.BudgetId = budgetId;
            if (account.Id == Guid.Empty)
                account.Id = Guid.NewGuid();

            await supabaseClient.From<ProfileBudgetBankAccount>()
                .Insert(account, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
