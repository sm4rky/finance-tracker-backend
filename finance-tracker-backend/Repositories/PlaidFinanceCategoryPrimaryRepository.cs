using finance_tracker_backend.Models;

namespace finance_tracker_backend.Repositories;

public sealed class PlaidFinanceCategoryPrimaryRepository(Supabase.Client supabaseClient)
    : IPlaidFinanceCategoryPrimaryRepository
{
    public async Task<IReadOnlyList<PlaidFinanceCategoryPrimary>> ListAsync(
        string? pfcVersion,
        CancellationToken cancellationToken = default)
    {
        var result = string.IsNullOrWhiteSpace(pfcVersion)
            ? await supabaseClient.From<PlaidFinanceCategoryPrimary>().Get(cancellationToken).ConfigureAwait(false)
            : await supabaseClient.From<PlaidFinanceCategoryPrimary>()
                .Where(x => x.PfcVersion == pfcVersion.Trim())
                .Get(cancellationToken)
                .ConfigureAwait(false);

        return result.Models
            .OrderBy(x => x.PfcVersion)
            .ThenBy(x => x.SortOrder)
            .ThenBy(x => x.Code, StringComparer.Ordinal)
            .ToList();
    }
}
