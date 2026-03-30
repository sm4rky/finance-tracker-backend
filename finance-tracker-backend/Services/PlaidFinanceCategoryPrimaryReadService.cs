using finance_tracker_backend.Contracts.Responses;
using finance_tracker_backend.Repositories;

namespace finance_tracker_backend.Services;

public sealed class PlaidFinanceCategoryPrimaryReadService(
    IPlaidFinanceCategoryPrimaryRepository plaidFinanceCategoryPrimaryRepository) : IPlaidFinanceCategoryPrimaryReadService
{
    public async Task<IReadOnlyList<PlaidFinanceCategoryPrimaryResponse>> ListAsync(
        string? pfcVersion,
        CancellationToken cancellationToken = default)
    {
        var rows = await plaidFinanceCategoryPrimaryRepository.ListAsync(pfcVersion, cancellationToken).ConfigureAwait(false);
        return rows.Select(r => new PlaidFinanceCategoryPrimaryResponse
        {
            PfcVersion = r.PfcVersion,
            Code = r.Code,
            DisplayName = r.DisplayName,
            SortOrder = r.SortOrder
        }).ToList();
    }
}
