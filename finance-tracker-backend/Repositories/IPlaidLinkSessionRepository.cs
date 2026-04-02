using finance_tracker_backend.Models;

namespace finance_tracker_backend.Repositories;

public interface IPlaidLinkSessionRepository
{
    Task<PlaidLinkSession?> GetByIdAndProfileAsync(Guid sessionId, Guid profileId, CancellationToken cancellationToken = default);
    Task<PlaidLinkSession?> GetByProfileAsync(Guid profileId, CancellationToken cancellationToken = default);
    Task InsertAsync(PlaidLinkSession session, CancellationToken cancellationToken = default);
    Task UpdateTokensAsync(
        Guid id,
        Guid profileId,
        string linkToken,
        DateTimeOffset expiresAt,
        string intent,
        CancellationToken cancellationToken = default);
    Task DeleteByIdAndProfileAsync(Guid sessionId, Guid profileId, CancellationToken cancellationToken = default);
    Task DeleteExpiredAsync(CancellationToken cancellationToken = default);
}
