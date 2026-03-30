using finance_tracker_backend.Enums;
using finance_tracker_backend.Models;

namespace finance_tracker_backend.Repositories;

public interface ITransactionRepository
{
    Task<Transaction?> GetByProfileAndPlaidTransactionIdAsync(
        Guid profileId,
        string plaidTransactionId,
        CancellationToken cancellationToken = default);

    Task InsertAsync(Transaction transaction, CancellationToken cancellationToken = default);

    Task UpdateAsync(Transaction transaction, CancellationToken cancellationToken = default);

    Task SetRemovedAtAsync(
        Guid profileId,
        string plaidTransactionId,
        DateTimeOffset removedAt,
        CancellationToken cancellationToken = default);

    Task<long> CountAsync(Guid profileId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Transaction>> QueryPagedAsync(
        Guid profileId,
        int offset,
        int limit,
        TransactionSortByField sortBy,
        bool descending,
        CancellationToken cancellationToken = default);
}
