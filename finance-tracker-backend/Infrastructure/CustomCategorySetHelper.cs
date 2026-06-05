using finance_tracker_backend.Contracts.Responses;
using finance_tracker_backend.Models;
using finance_tracker_backend.Repositories;

namespace finance_tracker_backend.Infrastructure;

public sealed class CustomCategorySetHelper(
    IProfileCustomCategorySetRepository customCategorySetRepository,
    IProfileCustomCategoryRepository customCategoryRepository,
    IProfileCustomCategoryPfcPrimaryRepository customCategoryPfcPrimaryRepository)
{
    public const string PfcVersion = "V2";

    public async Task<IReadOnlyDictionary<ProfileCustomCategoryPfcPrimary, ProfileCustomCategoryResponse>?> LoadCustomCategoryByPfcPrimaryAsync(
        Guid profileId,
        Guid customCategorySetId,
        IReadOnlyCollection<Guid>? customCategoryIds = null,
        CancellationToken cancellationToken = default)
    {
        var customCategorySet = await customCategorySetRepository
            .GetByIdAndProfileAsync(customCategorySetId, profileId, cancellationToken)
            .ConfigureAwait(false);
        if (customCategorySet is null)
            return null;

        if (customCategoryIds is { Count: 0 })
            return null;

        var customCategories = (await customCategoryRepository
                .ListBySetIdAsync(customCategorySetId, cancellationToken)
                .ConfigureAwait(false))
            .Where(category => customCategoryIds is null || customCategoryIds.Contains(category.Id))
            .ToList();

        var pfcPrimaries = customCategories.Count == 0
            ? []
            : await customCategoryPfcPrimaryRepository
                .ListByCategoryIdsAsync(customCategories.Select(category => category.Id).ToList(), cancellationToken)
                .ConfigureAwait(false);

        var pfcPrimariesByCustomCategoryId = pfcPrimaries
            .GroupBy(pfcPrimary => pfcPrimary.ProfileCustomCategoryId)
            .ToDictionary(
                group => group.Key,
                group => group.ToList());

        var customCategoriesById = customCategories
            .ToDictionary(profileCustomCategory => profileCustomCategory.Id);

        var customCategoryByPfcPrimary = pfcPrimaries.ToDictionary(
            pfcPrimary => pfcPrimary,
            pfcPrimary =>
            {
                var category = customCategoriesById[pfcPrimary.ProfileCustomCategoryId];
                var pfcPrimaryResponses = pfcPrimariesByCustomCategoryId[pfcPrimary.ProfileCustomCategoryId]
                    .Select(ToPfcPrimaryResponse)
                    .ToList();

                return ToCustomCategoryResponse(category, pfcPrimaryResponses);
            });

        return customCategoryByPfcPrimary;
    }

    public static ProfileCustomCategoryResponse? GetCustomCategoryResponseByPfcPrimaryCodeAndPfcVersion(
        IReadOnlyDictionary<ProfileCustomCategoryPfcPrimary, ProfileCustomCategoryResponse> customCategoryByPfcPrimary,
        string? pfcPrimaryCode,
        string? pfcVersion)
    {
        if (string.IsNullOrWhiteSpace(pfcPrimaryCode))
            return null;

        return customCategoryByPfcPrimary
            .FirstOrDefault(pair =>
                pair.Key.PfcPrimaryCode == pfcPrimaryCode &&
                pair.Key.PfcVersion == pfcVersion)
            .Value;
    }

    public static ProfileCustomCategoryResponse ToCustomCategoryResponse(
        ProfileCustomCategory category,
        IReadOnlyList<ProfileCustomCategoryPfcPrimaryResponse> pfcPrimaries) => new()
    {
        Id = category.Id,
        Name = category.Name,
        ColorSet = category.ColorSet,
        IconName = category.IconName,
        PfcPrimaries = pfcPrimaries
    };

    public static ProfileCustomCategoryPfcPrimaryResponse ToPfcPrimaryResponse(
        ProfileCustomCategoryPfcPrimary mapping) => new()
    {
        Id = mapping.Id,
        PfcPrimaryCode = mapping.PfcPrimaryCode,
        PfcVersion = mapping.PfcVersion
    };
}