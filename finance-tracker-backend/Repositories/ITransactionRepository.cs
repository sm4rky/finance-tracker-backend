using finance_tracker_backend.Enums;
using finance_tracker_backend.Models;
using finance_tracker_backend.Types;

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

    Task<long> CountAsync(
        Guid profileId,
        TransactionQueryFilters query,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Transaction>> QueryPagedAsync(
        Guid profileId,
        TransactionQueryFilters query,
        CancellationToken cancellationToken = default);
}
