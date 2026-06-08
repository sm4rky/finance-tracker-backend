using finance_tracker_backend.Models;

namespace finance_tracker_backend.Repositories;

public sealed class ProfileBudgetCategoryRepository(Supabase.Client supabaseClient) : IProfileBudgetCategoryRepository
{
    public async Task<IReadOnlyList<ProfileBudgetCategory>> ListByBudgetIdAsync(
        Guid budgetId,
        CancellationToken cancellationToken = default)
    {
        var result = await supabaseClient.From<ProfileBudgetCategory>()
            .Where(c => c.BudgetId == budgetId)
            .Get(cancellationToken)
            .ConfigureAwait(false);

        return result.Models;
    }

    public async Task<IReadOnlyList<ProfileBudgetCategory>> ListByBudgetIdsAsync(
        IReadOnlyCollection<Guid> budgetIds,
        CancellationToken cancellationToken = default)
    {
        if (budgetIds.Count == 0)
            return [];

        var categories = new List<ProfileBudgetCategory>();
        foreach (var budgetId in budgetIds.Distinct())
        {
            categories.AddRange(await ListByBudgetIdAsync(budgetId, cancellationToken).ConfigureAwait(false));
        }

        return categories;
    }

    public async Task ReplaceForBudgetAsync(
        Guid budgetId,
        IReadOnlyList<ProfileBudgetCategory> categories,
        CancellationToken cancellationToken = default)
    {
        await supabaseClient.From<ProfileBudgetCategory>()
            .Where(c => c.BudgetId == budgetId)
            .Delete(null, cancellationToken)
            .ConfigureAwait(false);

        foreach (var category in categories)
        {
            category.BudgetId = budgetId;
            if (category.Id == Guid.Empty)
                category.Id = Guid.NewGuid();

            await supabaseClient.From<ProfileBudgetCategory>()
                .Insert(category, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
