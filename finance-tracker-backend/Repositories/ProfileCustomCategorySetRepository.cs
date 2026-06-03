using finance_tracker_backend.Models;

namespace finance_tracker_backend.Repositories;

public sealed class ProfileCustomCategorySetRepository(Supabase.Client supabaseClient) : IProfileCustomCategorySetRepository
{
    public async Task<ProfileCustomCategorySet?> GetByIdAndProfileAsync(
        Guid id,
        Guid profileId,
        CancellationToken cancellationToken = default)
    {
        var result = await supabaseClient.From<ProfileCustomCategorySet>()
            .Where(s => s.Id == id)
            .Where(s => s.ProfileId == profileId)
            .Get(cancellationToken)
            .ConfigureAwait(false);

        return result.Models.Count > 0 ? result.Models[0] : null;
    }

    public async Task<IReadOnlyList<ProfileCustomCategorySet>> ListByProfileIdAsync(
        Guid profileId,
        CancellationToken cancellationToken = default)
    {
        var result = await supabaseClient.From<ProfileCustomCategorySet>()
            .Where(s => s.ProfileId == profileId)
            .Get(cancellationToken)
            .ConfigureAwait(false);

        return result.Models;
    }

    public async Task InsertAsync(ProfileCustomCategorySet set, CancellationToken cancellationToken = default)
    {
        await supabaseClient.From<ProfileCustomCategorySet>()
            .Insert(set, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task UpdateAsync(ProfileCustomCategorySet set, CancellationToken cancellationToken = default)
    {
        await supabaseClient.From<ProfileCustomCategorySet>()
            .Where(s => s.Id == set.Id)
            .Where(s => s.ProfileId == set.ProfileId)
            .Set(s => s.Name, set.Name)
            .Set(s => s.UpdatedAt, set.UpdatedAt)
            .Update(null, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task DeleteByIdAndProfileAsync(
        Guid id,
        Guid profileId,
        CancellationToken cancellationToken = default)
    {
        await supabaseClient.From<ProfileCustomCategorySet>()
            .Where(s => s.Id == id)
            .Where(s => s.ProfileId == profileId)
            .Delete(null, cancellationToken)
            .ConfigureAwait(false);
    }
}
