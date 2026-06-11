using finance_tracker_backend.Models;

namespace finance_tracker_backend.Repositories;

public sealed class PushLogRepository(Supabase.Client supabaseClient) : IPushLogRepository
{
    public async Task<bool> ExistsByProfileAndDedupeKeyAsync(
        Guid profileId,
        string dedupeKey,
        CancellationToken cancellationToken = default)
    {
        var result = await supabaseClient.From<PushLog>()
            .Where(x => x.ProfileId == profileId)
            .Where(x => x.DedupeKey == dedupeKey)
            .Get(cancellationToken)
            .ConfigureAwait(false);

        return result.Models.Count > 0;
    }

    public async Task InsertAsync(PushLog log, CancellationToken cancellationToken = default)
    {
        await supabaseClient.From<PushLog>().Insert(log, cancellationToken: cancellationToken).ConfigureAwait(false);
    }
}
