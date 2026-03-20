using finance_tracker_backend.Models;

namespace finance_tracker_backend.Repositories;

public sealed class ProfileSubscriptionRepository(Supabase.Client supabaseClient) : IProfileSubscriptionRepository
{
    public async Task<bool> ExistsForProfileAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
        var result = await supabaseClient.From<ProfileSubscription>()
            .Where(s => s.ProfileId == profileId)
            .Get(cancellationToken)
            .ConfigureAwait(false);
        return result.Models.Count > 0;
    }

    public async Task<ProfileSubscription?> GetForProfileAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
        var result = await supabaseClient.From<ProfileSubscription>()
            .Where(s => s.ProfileId == profileId)
            .Get(cancellationToken)
            .ConfigureAwait(false);
        return result.Models.Count > 0 ? result.Models[0] : null;
    }

    public async Task InsertAsync(ProfileSubscription subscription, CancellationToken cancellationToken = default)
    {
        await supabaseClient.From<ProfileSubscription>().Insert(subscription, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }
}
