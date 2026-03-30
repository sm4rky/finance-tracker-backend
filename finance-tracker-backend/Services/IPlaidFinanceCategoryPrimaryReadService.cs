using finance_tracker_backend.Contracts.Responses;

namespace finance_tracker_backend.Services;

public interface IPlaidFinanceCategoryPrimaryReadService
{
    Task<IReadOnlyList<PlaidFinanceCategoryPrimaryResponse>> ListAsync(
        string? pfcVersion,
        CancellationToken cancellationToken = default);
}
