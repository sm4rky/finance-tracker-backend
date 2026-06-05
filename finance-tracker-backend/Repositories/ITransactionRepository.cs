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

    Task<int> DeleteByIdsForProfileAsync(
        Guid profileId,
        IReadOnlyList<Guid> transactionIds,
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
        TransactionsQuery query,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Transaction>> QueryPagedAsync(
        Guid profileId,
        TransactionsQuery query,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Transaction>> ListRecentForProfileAsync(
        Guid profileId,
        int limit,
        CancellationToken cancellationToken = default);

    Task<(decimal TotalIncome, decimal TotalExpenses)> SumIncomeAndExpenseAsync(
        Guid profileId,
        TransactionsQuery query,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<(string? PfcPrimary, decimal TotalExpenses)>> SumExpensesByPfcPrimaryAsync(
        Guid profileId,
        TransactionsQuery query,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<(DateOnly PeriodStartDate, string? PfcPrimary, decimal ExpenseTotal)>>
        GetStackedExpensesByPfcPrimarySeriesAsync(
            Guid profileId,
            TransactionsQuery query,
            string timeGranularity,
            CancellationToken cancellationToken = default);

    Task<IReadOnlyList<(DateOnly PeriodStartDate, Guid? LinkedBankAccountId, string? OfficialName, decimal ExpenseTotal)>>
        GetGroupedExpensesByAccountSeriesAsync(
            Guid profileId,
            TransactionsQuery query,
            string timeGranularity,
            CancellationToken cancellationToken = default);
}
