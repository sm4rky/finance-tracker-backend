using finance_tracker_backend.Enums;
using finance_tracker_backend.Models;
using finance_tracker_backend.Types;

namespace finance_tracker_backend.Repositories;

public interface ITransactionRepository
{
    Task<Transaction?> GetByIdForProfileAsync(
        Guid profileId,
        Guid transactionId,
        CancellationToken cancellationToken = default);

    Task<Transaction?> GetByProfileAndPlaidTransactionIdAsync(
        Guid profileId,
        string plaidTransactionId,
        CancellationToken cancellationToken = default);

    Task<Transaction?> FindActiveDuplicateForFingerprintAsync(
        Guid profileId,
        DateOnly date,
        decimal amount,
        string normalizedMerchantFingerprint,
        string incomingPlaidTransactionId,
        CancellationToken cancellationToken = default);

    Task InsertAsync(Transaction transaction, CancellationToken cancellationToken = default);

    Task UpdateAsync(Transaction transaction, CancellationToken cancellationToken = default);

    Task SetRemovedAtAsync(
        Guid profileId,
        string plaidTransactionId,
        DateTimeOffset removedAt,
        CancellationToken cancellationToken = default);

    Task<int> DeleteByProfileAndLinkedBankIdAsync(
        Guid profileId,
        Guid linkedBankId,
        CancellationToken cancellationToken = default);

    Task<int> SetStatusByLinkedBankAccountIdAsync(
        Guid profileId,
        Guid linkedBankAccountId,
        string status,
        CancellationToken cancellationToken = default);

    Task<int> DeleteByProfileAndLinkedBankAccountIdAsync(
        Guid profileId,
        Guid linkedBankAccountId,
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
