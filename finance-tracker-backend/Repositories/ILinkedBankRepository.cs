using finance_tracker_backend.Models;

namespace finance_tracker_backend.Repositories;

public interface ILinkedBankRepository
{
    Task<LinkedBank?> GetByIdForProfileAsync(Guid id, Guid profileId, CancellationToken cancellationToken = default);

    Task<LinkedBank?> GetActiveByPlaidItemIdAsync(Guid profileId, string plaidItemId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LinkedBank>> ListByProfileIdAsync(Guid profileId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<(Guid ProfileId, Guid LinkedBankId)>> ListActiveLinkedBanksAfterIdAsync(
        Guid afterBankId,
        int limit,
        CancellationToken cancellationToken = default);

    Task InsertAsync(LinkedBank bank, CancellationToken cancellationToken = default);
    Task UpdateAsync(LinkedBank bank, CancellationToken cancellationToken = default);
    Task HardDeleteByIdForProfileAsync(Guid id, Guid profileId, CancellationToken cancellationToken = default);
}