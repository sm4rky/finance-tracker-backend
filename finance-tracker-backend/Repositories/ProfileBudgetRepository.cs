using finance_tracker_backend.Models;

namespace finance_tracker_backend.Repositories;

public sealed class ProfileBudgetRepository(Supabase.Client supabaseClient) : IProfileBudgetRepository
{
    public async Task<ProfileBudget?> GetByIdAndProfileAsync(
        Guid id,
        Guid profileId,
        CancellationToken cancellationToken = default)
    {
        var result = await supabaseClient.From<ProfileBudget>()
            .Where(b => b.Id == id)
            .Where(b => b.ProfileId == profileId)
            .Get(cancellationToken)
            .ConfigureAwait(false);

        return result.Models.Count > 0 ? result.Models[0] : null;
    }

    public async Task<ProfileBudget?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await supabaseClient.From<ProfileBudget>()
            .Where(b => b.Id == id)
            .Get(cancellationToken)
            .ConfigureAwait(false);

        return result.Models.Count > 0 ? result.Models[0] : null;
    }

    public async Task<IReadOnlyList<ProfileBudget>> ListByProfileIdAsync(
        Guid profileId,
        CancellationToken cancellationToken = default)
    {
        var result = await supabaseClient.From<ProfileBudget>()
            .Where(b => b.ProfileId == profileId)
            .Get(cancellationToken)
            .ConfigureAwait(false);

        return result.Models;
    }

    public async Task<IReadOnlyList<ProfileBudget>> ListActiveRecurringAsync(
        CancellationToken cancellationToken = default)
    {
        var result = await supabaseClient.From<ProfileBudget>()
            .Where(b => b.IsActive == true)
            .Where(b => b.IsRecurring == true)
            .Get(cancellationToken)
            .ConfigureAwait(false);

        return result.Models;
    }

    public async Task InsertAsync(ProfileBudget budget, CancellationToken cancellationToken = default)
    {
        await supabaseClient.From<ProfileBudget>()
            .Insert(budget, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task UpdateAsync(ProfileBudget budget, CancellationToken cancellationToken = default)
    {
        await supabaseClient.From<ProfileBudget>()
            .Where(b => b.Id == budget.Id)
            .Where(b => b.ProfileId == budget.ProfileId)
            .Set(b => b.Name, budget.Name)
            .Set(b => b.AmountLimit, budget.AmountLimit)
            .Set(b => b.IsActive, budget.IsActive)
            .Set(b => b.UpdatedAt, budget.UpdatedAt)
            .Update(null, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task DeactivateAsync(Guid id, DateTimeOffset updatedAt, CancellationToken cancellationToken = default)
    {
        await supabaseClient.From<ProfileBudget>()
            .Where(b => b.Id == id)
            .Set(b => b.IsActive, false)
            .Set(b => b.UpdatedAt, updatedAt)
            .Update(null, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task DeleteByIdAndProfileAsync(
        Guid id,
        Guid profileId,
        CancellationToken cancellationToken = default)
    {
        await supabaseClient.From<ProfileBudget>()
            .Where(b => b.Id == id)
            .Where(b => b.ProfileId == profileId)
            .Delete(null, cancellationToken)
            .ConfigureAwait(false);
    }
}
