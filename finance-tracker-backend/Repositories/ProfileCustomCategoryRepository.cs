using finance_tracker_backend.Models;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace finance_tracker_backend.Repositories;

public sealed class ProfileCustomCategoryRepository(
    Supabase.Client supabaseClient,
    IConfiguration configuration) : IProfileCustomCategoryRepository
{
    public async Task<IReadOnlyList<ProfileCustomCategory>> ListBySetIdAsync(
        Guid customCategorySetId,
        CancellationToken cancellationToken = default)
    {
        var result = await supabaseClient.From<ProfileCustomCategory>()
            .Where(c => c.ProfileCustomCategorySetId == customCategorySetId)
            .Get(cancellationToken)
            .ConfigureAwait(false);

        return result.Models;
    }

    public async Task<IReadOnlyList<ProfileCustomCategory>> ListBySetIdsAsync(
        IReadOnlyCollection<Guid> customCategorySetIds,
        CancellationToken cancellationToken = default)
    {
        if (customCategorySetIds.Count == 0)
            return [];

        var list = new List<ProfileCustomCategory>();
        foreach (var id in customCategorySetIds)
        {
            var chunk = await ListBySetIdAsync(id, cancellationToken).ConfigureAwait(false);
            list.AddRange(chunk);
        }

        return list;
    }

    public async Task InsertAsync(ProfileCustomCategory category, CancellationToken cancellationToken = default)
    {
        await supabaseClient.From<ProfileCustomCategory>()
            .Insert(category, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task UpdateAsync(ProfileCustomCategory category, CancellationToken cancellationToken = default)
    {
        await supabaseClient.From<ProfileCustomCategory>()
            .Where(c => c.Id == category.Id)
            .Where(c => c.ProfileCustomCategorySetId == category.ProfileCustomCategorySetId)
            .Set(c => c.Name, category.Name)
            .Set(c => c.ColorSet, category.ColorSet)
            .Set(c => c.IconName, category.IconName)
            .Update(null, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task DeleteMissingBySetIdAsync(
        Guid customCategorySetId,
        IReadOnlyCollection<Guid> keepCategoryIds,
        CancellationToken cancellationToken = default)
    {
        var sql = keepCategoryIds.Count == 0
            ? """
              DELETE FROM profile_custom_category
              WHERE profile_custom_category_set_id = @set_id
              """
            : """
              DELETE FROM profile_custom_category
              WHERE profile_custom_category_set_id = @set_id
                AND NOT (id = ANY(@keep_category_ids))
              """;

        await using var conn = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("set_id", customCategorySetId);
        if (keepCategoryIds.Count > 0)
            cmd.Parameters.AddWithValue("keep_category_ids", keepCategoryIds.ToArray());

        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<NpgsqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connectionString = configuration.GetConnectionString("Default");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:Default is required for profile custom category queries.");
        }

        var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(cancellationToken).ConfigureAwait(false);
        return conn;
    }
}
