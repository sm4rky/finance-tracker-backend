using finance_tracker_backend.Models;

namespace finance_tracker_backend.Repositories;

public interface IProfileRecurringCashflowRepository
{
    Task<ProfileRecurringCashflow?> GetByIdForProfileAsync(
        Guid profileId,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<ProfileRecurringCashflow?> GetByProfileAndPlaidStreamIdAsync(
        Guid profileId,
        string plaidStreamId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProfileRecurringCashflow>> ListForProfileAsync(
        Guid profileId,
        string? status,
        CancellationToken cancellationToken = default);

    Task InsertAsync(ProfileRecurringCashflow row, CancellationToken cancellationToken = default);

    Task UpdateAsync(ProfileRecurringCashflow row, CancellationToken cancellationToken = default);

    Task<int> DeleteForProfileAsync(Guid profileId, Guid id, CancellationToken cancellationToken = default);

    Task<int> SetStatusForLinkedBankAccountAsync(
        Guid profileId,
        Guid linkedBankAccountId,
        string status,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProfileRecurringCashflow>> ListByCalendarDateAsync(
        DateOnly calendarDate,
        int limit,
        CancellationToken cancellationToken = default);
}
