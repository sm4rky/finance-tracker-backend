using finance_tracker_backend.Models;

namespace finance_tracker_backend.Repositories;

public sealed class PlanRepository(Supabase.Client supabaseClient) : IPlanRepository
{
    public async Task<IReadOnlyList<SubscriptionPlan>> ListSubscriptionPlansAsync(
        CancellationToken cancellationToken = default)
    {
        var result = await supabaseClient.From<SubscriptionPlan>()
            .Get(cancellationToken)
            .ConfigureAwait(false);
        return result.Models;
    }

    public async Task<IReadOnlyList<SubscriptionPayment>> ListAllSubscriptionPaymentsAsync(
        CancellationToken cancellationToken = default)
    {
        var result = await supabaseClient.From<SubscriptionPayment>()
            .Get(cancellationToken)
            .ConfigureAwait(false);
        return result.Models;
    }

    public async Task<IReadOnlyList<SubscriptionPayment>> ListProfileSubscriptionPaymentsAsync(
        Guid profileId,
        CancellationToken cancellationToken = default)
    {
        var result = await supabaseClient.From<SubscriptionPayment>()
            .Where(x => x.ProfileId == profileId)
            .Get(cancellationToken)
            .ConfigureAwait(false);
        return result.Models;
    }

    public async Task<IReadOnlyList<SubscriptionHistory>> ListSubscriptionHistoryAsync(
        CancellationToken cancellationToken = default)
    {
        var result = await supabaseClient.From<SubscriptionHistory>()
            .Get(cancellationToken)
            .ConfigureAwait(false);
        return result.Models;
    }

    public async Task<IReadOnlyList<SubscriptionHistory>> ListSubscriptionHistoryForProfileAsync(
        Guid profileId,
        CancellationToken cancellationToken = default)
    {
        var result = await supabaseClient.From<SubscriptionHistory>()
            .Where(x => x.ProfileId == profileId)
            .Get(cancellationToken)
            .ConfigureAwait(false);
        return result.Models;
    }
}
