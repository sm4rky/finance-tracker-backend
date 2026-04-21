using finance_tracker_backend.Models;
using Supabase.Postgrest;

namespace finance_tracker_backend.Repositories;

public sealed class ProfileRepository(Supabase.Client supabaseClient) : IProfileRepository
{
    public async Task<bool> ExistsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var result = await supabaseClient.From<Profile>().Where(p => p.Id == userId).Get(cancellationToken)
            .ConfigureAwait(false);
        return result.Models.Count > 0;
    }

    public async Task<Profile?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var result = await supabaseClient.From<Profile>().Where(p => p.Id == userId).Get(cancellationToken)
            .ConfigureAwait(false);
        return result.Models.Count > 0 ? result.Models[0] : null;
    }

    public async Task InsertAsync(Profile profile, CancellationToken cancellationToken = default)
    {
        var options = new QueryOptions
        {
            DuplicateResolution = QueryOptions.DuplicateResolutionType.IgnoreDuplicates
        };
        await supabaseClient.From<Profile>().Insert(profile, options, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Guid>> ListAllProfileIdsAsync(CancellationToken cancellationToken = default)
    {
        var result = await supabaseClient.From<Profile>().Get(cancellationToken).ConfigureAwait(false);
        return result.Models.Select(p => p.Id).ToList();
    }
}
