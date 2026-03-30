using finance_tracker_backend.Models;

namespace finance_tracker_backend.Repositories;

public interface IPlaidFinanceCategoryPrimaryRepository
{
    Task<IReadOnlyList<PlaidFinanceCategoryPrimary>> ListAsync(
        string? pfcVersion,
        CancellationToken cancellationToken = default);
}
