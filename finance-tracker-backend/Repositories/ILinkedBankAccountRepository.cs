using finance_tracker_backend.Models;

namespace finance_tracker_backend.Repositories;

public interface ILinkedBankAccountRepository
{
    Task<IReadOnlyList<LinkedBankAccount>> ListByLinkedBankIdAsync(Guid linkedBankId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LinkedBankAccount>> ListByLinkedBankIdsAsync(IReadOnlyCollection<Guid> linkedBankIds, CancellationToken cancellationToken = default);
    Task InsertAsync(LinkedBankAccount account, CancellationToken cancellationToken = default);
    Task UpdateAsync(LinkedBankAccount account, CancellationToken cancellationToken = default);
}
