using System.Security.Claims;
using finance_tracker_backend.Contracts.Requests;
using finance_tracker_backend.Contracts.Responses;
using finance_tracker_backend.Infrastructure;
using finance_tracker_backend.Models;
using finance_tracker_backend.Repositories;

namespace finance_tracker_backend.Services;

public sealed class ProfileCustomCategorySetService(
    IProfileCustomCategorySetRepository customCategorySetRepository,
    IProfileCustomCategoryRepository customCategoryRepository,
    IProfileCustomCategoryPfcPrimaryRepository customCategoryPfcPrimaryRepository,
    IPlaidFinanceCategoryPrimaryRepository plaidFinanceCategoryPrimaryRepository) : IProfileCustomCategorySetService
{
    public async Task<IReadOnlyList<ProfileCustomCategorySetResponse>> ListAsync(
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        var profileId = user.RequireProfileId();
        var sets = await customCategorySetRepository.ListByProfileIdAsync(profileId, cancellationToken).ConfigureAwait(false);
        return await MapSetsAsync(sets, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ProfileCustomCategorySetResponse> UpsertAsync(
        ClaimsPrincipal user,
        UpsertProfileCustomCategorySetRequest request,
        CancellationToken cancellationToken = default)
    {
        var profileId = user.RequireProfileId();
        var normalized = await NormalizeAndValidateAsync(request, cancellationToken).ConfigureAwait(false);
        var now = DateTimeOffset.UtcNow;

        ProfileCustomCategorySet set;
        if (request.Id is null)
        {
            set = new ProfileCustomCategorySet
            {
                Id = Guid.NewGuid(),
                ProfileId = profileId,
                Name = normalized.Name,
                CreatedAt = now,
                UpdatedAt = now
            };
            await customCategorySetRepository.InsertAsync(set, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            set = await customCategorySetRepository
                .GetByIdAndProfileAsync(request.Id.Value, profileId, cancellationToken)
                .ConfigureAwait(false)
                ?? throw new KeyNotFoundException("Custom category set was not found.");

            set.Name = normalized.Name;
            set.UpdatedAt = now;
            await customCategorySetRepository.UpdateAsync(set, cancellationToken).ConfigureAwait(false);
        }

        var existingCategories = await customCategoryRepository.ListBySetIdAsync(set.Id, cancellationToken)
            .ConfigureAwait(false);
        var existingById = existingCategories.ToDictionary(c => c.Id);
        var keepCategoryIds = new List<Guid>();

        foreach (var categoryInput in normalized.Categories)
        {
            ProfileCustomCategory category;
            if (categoryInput.Id is { } categoryId)
            {
                if (!existingById.TryGetValue(categoryId, out category!))
                    throw new KeyNotFoundException("Custom category was not found.");

                category.Name = categoryInput.Name;
                category.ColorSet = categoryInput.ColorSet;
                category.IconName = categoryInput.IconName;
                await customCategoryRepository.UpdateAsync(category, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                category = new ProfileCustomCategory
                {
                    Id = Guid.NewGuid(),
                    ProfileCustomCategorySetId = set.Id,
                    Name = categoryInput.Name,
                    ColorSet = categoryInput.ColorSet,
                    IconName = categoryInput.IconName
                };
                await customCategoryRepository.InsertAsync(category, cancellationToken).ConfigureAwait(false);
            }

            keepCategoryIds.Add(category.Id);
            var mappings = categoryInput.PfcPrimaries
                .Select(p => new ProfileCustomCategoryPfcPrimary
                {
                    Id = Guid.NewGuid(),
                    ProfileCustomCategoryId = category.Id,
                    PfcPrimaryCode = p.PfcPrimaryCode,
                    PfcVersion = p.PfcVersion
                })
                .ToList();

            await customCategoryPfcPrimaryRepository
                .ReplaceForCategoryAsync(category.Id, mappings, cancellationToken)
                .ConfigureAwait(false);
        }

        await customCategoryRepository.DeleteMissingBySetIdAsync(set.Id, keepCategoryIds, cancellationToken)
            .ConfigureAwait(false);

        return await GetResponseForSetAsync(profileId, set.Id, cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(
        ClaimsPrincipal user,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var profileId = user.RequireProfileId();
        var existing = await customCategorySetRepository.GetByIdAndProfileAsync(id, profileId, cancellationToken)
            .ConfigureAwait(false);

        if (existing is null)
            throw new KeyNotFoundException("Custom category set was not found.");

        await customCategorySetRepository.DeleteByIdAndProfileAsync(id, profileId, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<NormalizedProfileCustomCategorySet> NormalizeAndValidateAsync(
        UpsertProfileCustomCategorySetRequest request,
        CancellationToken cancellationToken)
    {
        var name = TrimRequired(request.Name, "name is required.");
        var categories = request.Categories ?? throw new ArgumentException("categories is required.");

        var categoryIds = new HashSet<Guid>();
        var normalizedCategories = new List<NormalizedProfileCustomCategory>();
        var requestedPfcKeys = new HashSet<(string Code, string Version)>();

        foreach (var category in categories)
        {
            if (category.Id is { } categoryId && !categoryIds.Add(categoryId))
                throw new ArgumentException("Duplicate category id in request.");

            var mappingKeys = new HashSet<(string Code, string Version)>();
            var normalizedMappings = new List<NormalizedProfileCustomCategoryPfcPrimary>();
            var pfcPrimaries = category.PfcPrimaries ?? throw new ArgumentException("pfcPrimaries is required.");

            foreach (var mapping in pfcPrimaries)
            {
                var code = TrimRequired(mapping.PfcPrimaryCode, "pfcPrimaryCode is required.").ToUpperInvariant();
                var version = TrimRequired(mapping.PfcVersion, "pfcVersion is required.").ToUpperInvariant();
                var key = (code, version);

                if (!mappingKeys.Add(key))
                    throw new ArgumentException("Duplicate pfcPrimary mapping in category.");

                requestedPfcKeys.Add(key);
                normalizedMappings.Add(new NormalizedProfileCustomCategoryPfcPrimary
                {
                    PfcPrimaryCode = code,
                    PfcVersion = version
                });
            }

            normalizedCategories.Add(new NormalizedProfileCustomCategory
            {
                Id = category.Id,
                Name = TrimRequired(category.Name, "category name is required."),
                ColorSet = TrimRequired(category.ColorSet, "colorSet is required."),
                IconName = TrimRequired(category.IconName, "iconName is required."),
                PfcPrimaries = normalizedMappings
            });
        }

        if (requestedPfcKeys.Count > 0)
        {
            var plaidPrimaries = await plaidFinanceCategoryPrimaryRepository
                .ListAsync(pfcVersion: null, cancellationToken)
                .ConfigureAwait(false);
            var validPfcKeys = plaidPrimaries
                .Select(p => (p.Code, p.PfcVersion))
                .ToHashSet();

            foreach (var category in normalizedCategories)
            {
                category.PfcPrimaries = category.PfcPrimaries
                    .Where(p => validPfcKeys.Contains((p.PfcPrimaryCode, p.PfcVersion)))
                    .ToList();
            }
        }

        return new NormalizedProfileCustomCategorySet
        {
            Name = name,
            Categories = normalizedCategories
        };
    }

    private async Task<ProfileCustomCategorySetResponse> GetResponseForSetAsync(
        Guid profileId,
        Guid setId,
        CancellationToken cancellationToken)
    {
        var set = await customCategorySetRepository.GetByIdAndProfileAsync(setId, profileId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new KeyNotFoundException("Custom category set was not found.");

        var responses = await MapSetsAsync([set], cancellationToken).ConfigureAwait(false);
        return responses[0];
    }

    private async Task<IReadOnlyList<ProfileCustomCategorySetResponse>> MapSetsAsync(
        IReadOnlyList<ProfileCustomCategorySet> sets,
        CancellationToken cancellationToken)
    {
        var orderedSets = sets
            .OrderByDescending(s => s.UpdatedAt)
            .ThenBy(s => s.Id)
            .ToList();

        var setIds = orderedSets.Select(s => s.Id).ToList();
        var categories = await customCategoryRepository.ListBySetIdsAsync(setIds, cancellationToken).ConfigureAwait(false);
        var orderedCategories = categories
            .OrderBy(c => c.Name)
            .ThenBy(c => c.Id)
            .ToList();

        var categoryIds = orderedCategories.Select(c => c.Id).ToList();
        var mappings = await customCategoryPfcPrimaryRepository
            .ListByCategoryIdsAsync(categoryIds, cancellationToken)
            .ConfigureAwait(false);
        var mappingsByCategory = mappings
            .GroupBy(m => m.ProfileCustomCategoryId)
            .ToDictionary(
                g => g.Key,
                g => g
                    .OrderBy(m => m.PfcVersion)
                    .ThenBy(m => m.PfcPrimaryCode)
                    .Select(CustomCategorySetHelper.ToPfcPrimaryResponse)
                    .ToList());
        var categoriesBySet = orderedCategories
            .GroupBy(c => c.ProfileCustomCategorySetId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(c => CustomCategorySetHelper.ToCustomCategoryResponse(
                    c,
                    mappingsByCategory.GetValueOrDefault(c.Id) ?? [])).ToList());

        return orderedSets.Select(s => new ProfileCustomCategorySetResponse
        {
            Id = s.Id,
            Name = s.Name,
            CreatedAt = s.CreatedAt,
            UpdatedAt = s.UpdatedAt,
            Categories = categoriesBySet.GetValueOrDefault(s.Id) ?? []
        }).ToList();
    }

    private static string TrimRequired(string? value, string message)
    {
        return string.IsNullOrWhiteSpace(value) ? throw new ArgumentException(message) : value.Trim();
    }

    private sealed class NormalizedProfileCustomCategorySet
    {
        public string Name { get; init; } = string.Empty;
        public IReadOnlyList<NormalizedProfileCustomCategory> Categories { get; init; } = [];
    }

    private sealed class NormalizedProfileCustomCategory
    {
        public Guid? Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string ColorSet { get; init; } = string.Empty;
        public string IconName { get; init; } = string.Empty;
        public IReadOnlyList<NormalizedProfileCustomCategoryPfcPrimary> PfcPrimaries { get; set; } = [];
    }

    private sealed class NormalizedProfileCustomCategoryPfcPrimary
    {
        public string PfcPrimaryCode { get; init; } = string.Empty;
        public string PfcVersion { get; init; } = string.Empty;
    }
}
