using finance_tracker_backend.Models;

namespace finance_tracker_backend.Repositories;

public sealed class PlaidLinkSessionRepository(Supabase.Client supabaseClient) : IPlaidLinkSessionRepository
{
    public async Task<PlaidLinkSession?> GetByIdAndProfileAsync(
        Guid sessionId,
        Guid profileId,
        CancellationToken cancellationToken = default)
    {
        var result = await supabaseClient.From<PlaidLinkSession>()
            .Where(s => s.Id == sessionId)
            .Where(s => s.ProfileId == profileId)
            .Get(cancellationToken)
            .ConfigureAwait(false);
        return result.Models.Count > 0 ? result.Models[0] : null;
    }

    public async Task<PlaidLinkSession?> GetByProfileAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
        var result = await supabaseClient.From<PlaidLinkSession>()
            .Where(s => s.ProfileId == profileId)
            .Get(cancellationToken)
            .ConfigureAwait(false);
        return result.Models.Count > 0 ? result.Models[0] : null;
    }

    public async Task InsertAsync(PlaidLinkSession session, CancellationToken cancellationToken = default)
    {
        await supabaseClient.From<PlaidLinkSession>().Insert(session, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task UpdateTokensAsync(
        Guid id,
        Guid profileId,
        string linkToken,
        DateTimeOffset expiresAt,
        string intent,
        CancellationToken cancellationToken = default)
    {
        await supabaseClient.From<PlaidLinkSession>()
            .Where(s => s.Id == id)
            .Where(s => s.ProfileId == profileId)
            .Set(s => s.LinkToken, linkToken)
            .Set(s => s.ExpiresAt, expiresAt)
            .Set(s => s.Intent, intent)
            .Set(s => s.CreatedAt, DateTimeOffset.UtcNow)
            .Update(null, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task DeleteByIdAndProfileAsync(Guid sessionId, Guid profileId, CancellationToken cancellationToken = default)
    {
        await supabaseClient.From<PlaidLinkSession>()
            .Where(s => s.Id == sessionId)
            .Where(s => s.ProfileId == profileId)
            .Delete(null, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task DeleteExpiredAsync(CancellationToken cancellationToken = default)
    {
        await supabaseClient.From<PlaidLinkSession>()
            .Where(s => s.ExpiresAt <= DateTimeOffset.UtcNow)
            .Delete(null, cancellationToken)
            .ConfigureAwait(false);
    }
}
