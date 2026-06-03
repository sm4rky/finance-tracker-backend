using finance_tracker_backend.Models;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace finance_tracker_backend.Repositories;

public sealed class ProfileCustomCategoryPfcPrimaryRepository(
    Supabase.Client supabaseClient,
    IConfiguration configuration) : IProfileCustomCategoryPfcPrimaryRepository
{
    public async Task<IReadOnlyList<ProfileCustomCategoryPfcPrimary>> ListByCategoryIdAsync(
        Guid customCategoryId,
        CancellationToken cancellationToken = default)
    {
        var result = await supabaseClient.From<ProfileCustomCategoryPfcPrimary>()
            .Where(m => m.ProfileCustomCategoryId == customCategoryId)
            .Get(cancellationToken)
            .ConfigureAwait(false);

        return result.Models;
    }

    public async Task<IReadOnlyList<ProfileCustomCategoryPfcPrimary>> ListByCategoryIdsAsync(
        IReadOnlyCollection<Guid> customCategoryIds,
        CancellationToken cancellationToken = default)
    {
        if (customCategoryIds.Count == 0)
            return [];

        var list = new List<ProfileCustomCategoryPfcPrimary>();
        foreach (var id in customCategoryIds)
        {
            var chunk = await ListByCategoryIdAsync(id, cancellationToken).ConfigureAwait(false);
            list.AddRange(chunk);
        }

        return list;
    }

    public async Task ReplaceForCategoryAsync(
        Guid customCategoryId,
        IReadOnlyList<ProfileCustomCategoryPfcPrimary> mappings,
        CancellationToken cancellationToken = default)
    {
        var existingIds = await LoadExistingMappingIdsAsync(customCategoryId, cancellationToken).ConfigureAwait(false);

        await supabaseClient.From<ProfileCustomCategoryPfcPrimary>()
            .Where(m => m.ProfileCustomCategoryId == customCategoryId)
            .Delete(null, cancellationToken)
            .ConfigureAwait(false);

        foreach (var mapping in mappings)
        {
            var key = (mapping.PfcPrimaryCode, mapping.PfcVersion);
            mapping.Id = existingIds.GetValueOrDefault(key, mapping.Id == Guid.Empty ? Guid.NewGuid() : mapping.Id);
            mapping.ProfileCustomCategoryId = customCategoryId;

            await supabaseClient.From<ProfileCustomCategoryPfcPrimary>()
                .Insert(mapping, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private async Task<Dictionary<(string Code, string Version), Guid>> LoadExistingMappingIdsAsync(
        Guid customCategoryId,
        CancellationToken cancellationToken)
    {
        const string sql =
            """
            SELECT id, pfc_primary_code, pfc_version
            FROM profile_custom_category_pfc_primary
            WHERE profile_custom_category_id = @category_id
            """;

        await using var conn = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("category_id", customCategoryId);

        var map = new Dictionary<(string Code, string Version), Guid>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            map[(reader.GetString(1), reader.GetString(2))] = reader.GetGuid(0);
        }

        return map;
    }

    private async Task<NpgsqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connectionString = configuration.GetConnectionString("Default");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:Default is required for profile custom category pfc primary queries.");
        }

        var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(cancellationToken).ConfigureAwait(false);
        return conn;
    }
}
