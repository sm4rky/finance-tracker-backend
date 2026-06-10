using System.Globalization;
using System.Security.Claims;
using finance_tracker_backend.Contracts.Requests;
using finance_tracker_backend.Contracts.Responses;
using finance_tracker_backend.Infrastructure;
using finance_tracker_backend.Models;
using finance_tracker_backend.Repositories;

namespace finance_tracker_backend.Services;

public sealed class ProfileBudgetService(
    IProfileBudgetRepository budgetRepository,
    IProfileBudgetCategoryRepository budgetCategoryRepository,
    IProfileBudgetBankAccountRepository budgetBankAccountRepository,
    IProfileBudgetPeriodRepository budgetPeriodRepository,
    IProfileCustomCategorySetRepository customCategorySetRepository,
    IProfileCustomCategoryRepository customCategoryRepository,
    IProfileCustomCategoryPfcPrimaryRepository customCategoryPfcPrimaryRepository,
    IPlaidFinanceCategoryPrimaryRepository plaidFinanceCategoryPrimaryRepository,
    ILinkedBankAccountRepository linkedBankAccountRepository,
    ILinkedBankRepository linkedBankRepository,
    IBudgetPeriodMaintenanceService budgetPeriodMaintenanceService) : IProfileBudgetService
{
    public async Task<IReadOnlyList<ProfileBudgetResponse>> ListAsync(
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        var profileId = user.RequireProfileId();
        var budgets = await budgetRepository.ListByProfileIdAsync(profileId, cancellationToken).ConfigureAwait(false);
        return await MapBudgetsAsync(budgets, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ProfileBudgetResponse> GetByIdAsync(
        ClaimsPrincipal user,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var profileId = user.RequireProfileId();
        var budget = await budgetRepository.GetByIdAndProfileAsync(id, profileId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new KeyNotFoundException("Budget was not found.");

        var responses = await MapBudgetsAsync([budget], cancellationToken).ConfigureAwait(false);
        return responses[0];
    }

    public async Task<IReadOnlyList<ProfileBudgetPeriodResponse>> ListPeriodsAsync(
        ClaimsPrincipal user,
        Guid budgetId,
        CancellationToken cancellationToken = default)
    {
        var profileId = user.RequireProfileId();
        _ = await budgetRepository.GetByIdAndProfileAsync(budgetId, profileId, cancellationToken)
                .ConfigureAwait(false)
            ?? throw new KeyNotFoundException("Budget was not found.");

        var periods = await budgetPeriodRepository.ListByBudgetIdAsync(budgetId, cancellationToken)
            .ConfigureAwait(false);

        return periods.Select(ToPeriodResponse).ToList();
    }

    public async Task<IReadOnlyList<ProfileBudgetPeriodResponse>> ListOngoingPeriodsAsync(
        ClaimsPrincipal user,
        int? limit = null,
        CancellationToken cancellationToken = default)
    {
        var profileId = user.RequireProfileId();
        var take = limit switch
        {
            null => 5,
            < 1 => throw new ArgumentException("limit must be at least 1."),
            _ => limit.Value
        };

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var periods = await budgetPeriodRepository
            .ListActiveBudgetPeriodsContainingAnyDateAsync(profileId, [today], cancellationToken)
            .ConfigureAwait(false);

        return periods
            .OrderBy(p => p.PeriodEndDate)
            .ThenBy(p => p.PeriodName)
            .Take(take)
            .Select(ToPeriodResponse)
            .ToList();
    }

    public async Task<ProfileBudgetResponse> CreateAsync(
        ClaimsPrincipal user,
        CreateProfileBudgetRequest request,
        CancellationToken cancellationToken = default)
    {
        var profileId = user.RequireProfileId();
        var normalized = await NormalizeCreateRequestAsync(profileId, request, cancellationToken).ConfigureAwait(false);
        var now = DateTimeOffset.UtcNow;

        var budget = new ProfileBudget
        {
            Id = Guid.NewGuid(),
            ProfileId = profileId,
            Name = normalized.Name,
            AmountLimit = normalized.AmountLimit,
            IsRecurring = normalized.IsRecurring,
            PeriodType = normalized.PeriodType,
            StartDate = normalized.StartDate,
            EndDate = normalized.EndDate,
            IsActive = true,
            ProfileCustomCategorySetId = normalized.ProfileCustomCategorySetId,
            IncludeIncome = normalized.IncludeIncome,
            IncludeUnlinkedTransactions = normalized.IncludeUnlinkedTransactions,
            CreatedAt = now,
            UpdatedAt = now
        };

        await budgetRepository.InsertAsync(budget, cancellationToken).ConfigureAwait(false);
        await budgetCategoryRepository
            .ReplaceForBudgetAsync(budget.Id, normalized.Categories, cancellationToken)
            .ConfigureAwait(false);
        await budgetBankAccountRepository
            .ReplaceForBudgetAsync(budget.Id, normalized.BankAccounts, cancellationToken)
            .ConfigureAwait(false);
        await budgetPeriodMaintenanceService
            .EnsureInitialPeriodsAsync(budget, cancellationToken)
            .ConfigureAwait(false);

        return await GetByIdAsync(user, budget.Id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ProfileBudgetResponse> UpdateAsync(
        ClaimsPrincipal user,
        Guid id,
        UpdateProfileBudgetRequest request,
        CancellationToken cancellationToken = default)
    {
        var profileId = user.RequireProfileId();
        var budget = await budgetRepository.GetByIdAndProfileAsync(id, profileId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new KeyNotFoundException("Budget was not found.");

        if (request.AmountLimit < 0)
            throw new ArgumentException("amountLimit must be greater than or equal to 0.");

        var name = TrimRequired(request.Name, "name is required.");
        var now = DateTimeOffset.UtcNow;
        var amountLimitChanged = budget.AmountLimit != request.AmountLimit;

        budget.Name = name;
        budget.AmountLimit = request.AmountLimit;
        budget.IsActive = request.IsActive;
        budget.UpdatedAt = now;

        await budgetRepository.UpdateAsync(budget, cancellationToken).ConfigureAwait(false);

        if (amountLimitChanged && budget.IsActive)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var currentPeriod = await budgetPeriodRepository
                .GetCurrentPeriodAsync(budget.Id, today, cancellationToken)
                .ConfigureAwait(false);

            if (currentPeriod is not null)
            {
                await budgetPeriodRepository
                    .UpdateAmountLimitAsync(currentPeriod.Id, request.AmountLimit, now, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        return await GetByIdAsync(user, budget.Id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ProfileBudgetResponse> UpdateActiveAsync(
        ClaimsPrincipal user,
        Guid id,
        UpdateProfileBudgetActiveRequest request,
        CancellationToken cancellationToken = default)
    {
        var profileId = user.RequireProfileId();
        var budget = await budgetRepository.GetByIdAndProfileAsync(id, profileId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new KeyNotFoundException("Budget was not found.");

        budget.IsActive = request.IsActive;
        budget.UpdatedAt = DateTimeOffset.UtcNow;

        await budgetRepository.UpdateAsync(budget, cancellationToken).ConfigureAwait(false);
        return await GetByIdAsync(user, budget.Id, cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(
        ClaimsPrincipal user,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var profileId = user.RequireProfileId();
        _ = await budgetRepository.GetByIdAndProfileAsync(id, profileId, cancellationToken)
                .ConfigureAwait(false)
            ?? throw new KeyNotFoundException("Budget was not found.");

        await budgetRepository.DeleteByIdAndProfileAsync(id, profileId, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<ProfileBudgetResponse>> MapBudgetsAsync(
        IReadOnlyList<ProfileBudget> budgets,
        CancellationToken cancellationToken)
    {
        if (budgets.Count == 0)
            return [];

        var orderedBudgets = budgets
            .OrderByDescending(b => b.UpdatedAt)
            .ThenBy(b => b.Name)
            .ToList();
        var budgetIds = orderedBudgets.Select(b => b.Id).ToList();
        var categories = await budgetCategoryRepository.ListByBudgetIdsAsync(budgetIds, cancellationToken)
            .ConfigureAwait(false);
        var accounts = await budgetBankAccountRepository.ListByBudgetIdsAsync(budgetIds, cancellationToken)
            .ConfigureAwait(false);
        var categoriesByBudget = categories.GroupBy(c => c.BudgetId).ToDictionary(g => g.Key, g => g.ToList());
        var accountsByBudget = accounts.GroupBy(a => a.BudgetId).ToDictionary(g => g.Key, g => g.ToList());
        var customCategoryResponsesById = await LoadCustomCategoryResponsesByIdAsync(
                orderedBudgets,
                categories,
                cancellationToken)
            .ConfigureAwait(false);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var responses = new List<ProfileBudgetResponse>();
        foreach (var budget in orderedBudgets)
        {
            var currentPeriod = await budgetPeriodRepository
                .GetCurrentPeriodAsync(budget.Id, today, cancellationToken)
                .ConfigureAwait(false);

            responses.Add(new ProfileBudgetResponse
            {
                Id = budget.Id,
                Name = budget.Name,
                AmountLimit = budget.AmountLimit,
                IsRecurring = budget.IsRecurring,
                PeriodType = budget.PeriodType,
                StartDate = budget.StartDate,
                EndDate = budget.EndDate,
                IsActive = budget.IsActive,
                ProfileCustomCategorySetId = budget.ProfileCustomCategorySetId,
                IncludeIncome = budget.IncludeIncome,
                IncludeUnlinkedTransactions = budget.IncludeUnlinkedTransactions,
                CreatedAt = budget.CreatedAt,
                UpdatedAt = budget.UpdatedAt,
                Categories = (categoriesByBudget.GetValueOrDefault(budget.Id) ?? [])
                    .Select(category => ToCategoryResponse(category, customCategoryResponsesById))
                    .ToList(),
                LinkedBankAccountIds = (accountsByBudget.GetValueOrDefault(budget.Id) ?? [])
                    .Select(a => a.LinkedBankAccountId)
                    .ToList(),
                CurrentPeriod = currentPeriod is null ? null : ToPeriodResponse(currentPeriod)
            });
        }

        return responses;
    }

    private async Task<IReadOnlyDictionary<Guid, ProfileCustomCategoryResponse>> LoadCustomCategoryResponsesByIdAsync(
        IReadOnlyList<ProfileBudget> budgets,
        IReadOnlyList<ProfileBudgetCategory> budgetCategories,
        CancellationToken cancellationToken)
    {
        var customCategoryIds = budgetCategories
            .Where(category => category.CustomCategoryId is not null)
            .Select(category => category.CustomCategoryId!.Value)
            .Distinct()
            .ToHashSet();
        if (customCategoryIds.Count == 0)
            return new Dictionary<Guid, ProfileCustomCategoryResponse>();

        var customCategorySetIds = budgets
            .Where(budget => budget.ProfileCustomCategorySetId is not null)
            .Select(budget => budget.ProfileCustomCategorySetId!.Value)
            .Distinct()
            .ToList();
        if (customCategorySetIds.Count == 0)
            return new Dictionary<Guid, ProfileCustomCategoryResponse>();

        var customCategories = (await customCategoryRepository
                .ListBySetIdsAsync(customCategorySetIds, cancellationToken)
                .ConfigureAwait(false))
            .Where(category => customCategoryIds.Contains(category.Id))
            .ToList();
        if (customCategories.Count == 0)
            return new Dictionary<Guid, ProfileCustomCategoryResponse>();

        var pfcPrimaries = await customCategoryPfcPrimaryRepository
            .ListByCategoryIdsAsync(customCategories.Select(category => category.Id).ToList(), cancellationToken)
            .ConfigureAwait(false);
        var pfcPrimariesByCategoryId = pfcPrimaries
            .GroupBy(pfcPrimary => pfcPrimary.ProfileCustomCategoryId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(CustomCategorySetHelper.ToPfcPrimaryResponse)
                    .ToList());

        return customCategories.ToDictionary(
            category => category.Id,
            category =>
            {
                var pfcPrimaryResponses = pfcPrimariesByCategoryId.GetValueOrDefault(category.Id) ?? [];
                return CustomCategorySetHelper.ToCustomCategoryResponse(category, pfcPrimaryResponses);
            });
    }

    private async Task<NormalizedCreateProfileBudget> NormalizeCreateRequestAsync(
        Guid profileId,
        CreateProfileBudgetRequest request,
        CancellationToken cancellationToken)
    {
        var name = TrimRequired(request.Name, "name is required.");
        if (request.AmountLimit < 0)
            throw new ArgumentException("amountLimit must be greater than or equal to 0.");

        var periodType = request.IsRecurring
            ? BudgetPeriodHelper.ParsePeriodType(request.PeriodType)
            : null;
        var startDate = ParseRequiredDate(request.StartDate, "startDate");
        DateOnly? endDate = string.IsNullOrWhiteSpace(request.EndDate)
            ? null
            : ParseRequiredDate(request.EndDate, "endDate");

        if (!request.IsRecurring)
        {
            if (endDate is null)
                throw new ArgumentException("endDate is required for fixed budgets.");

            if (endDate < startDate)
                throw new ArgumentException("endDate must be on or after startDate.");
        }
        else if (endDate is { } recurringEndDate && recurringEndDate < startDate)
        {
            throw new ArgumentException("endDate must be on or after startDate.");
        }

        if (request.Categories is null || request.Categories.Count == 0)
            throw new ArgumentException("categories must contain at least one item.");

        if (request.LinkedBankAccountIds is null || request.LinkedBankAccountIds.Count == 0)
            throw new ArgumentException("linkedBankAccountIds must contain at least one id.");

        Guid? customCategorySetId = null;
        if (request.ProfileCustomCategorySetId is { } setId)
        {
            var set = await customCategorySetRepository
                .GetByIdAndProfileAsync(setId, profileId, cancellationToken)
                .ConfigureAwait(false)
                ?? throw new KeyNotFoundException("Custom category set was not found.");

            customCategorySetId = set.Id;
        }

        var plaidPrimaries = await plaidFinanceCategoryPrimaryRepository
            .ListAsync(pfcVersion: null, cancellationToken)
            .ConfigureAwait(false);
        var validPfcKeys = plaidPrimaries
            .Select(p => (p.Code, p.PfcVersion))
            .ToHashSet();

        var customCategoriesInSet = customCategorySetId is { } categorySetId
            ? (await customCategoryRepository.ListBySetIdAsync(categorySetId, cancellationToken).ConfigureAwait(false))
            .ToDictionary(c => c.Id)
            : [];

        var normalizedCategories = new List<ProfileBudgetCategory>();
        var categoryKeys = new HashSet<string>(StringComparer.Ordinal);

        foreach (var category in request.Categories)
        {
            if (category.CustomCategoryId is { } customCategoryId)
            {
                if (customCategorySetId is null)
                    throw new ArgumentException("profileCustomCategorySetId is required when using custom categories.");

                if (!customCategoriesInSet.ContainsKey(customCategoryId))
                    throw new ArgumentException("customCategoryId does not belong to the selected custom category set.");

                var key = $"custom:{customCategoryId}";
                if (!categoryKeys.Add(key))
                    throw new ArgumentException("Duplicate custom category in request.");

                normalizedCategories.Add(new ProfileBudgetCategory
                {
                    Id = Guid.NewGuid(),
                    CustomCategoryId = customCategoryId
                });
                continue;
            }

            var code = TrimRequired(category.PfcPrimaryCode, "pfcPrimaryCode is required.").ToUpperInvariant();
            var version = TrimRequired(category.PfcVersion, "pfcVersion is required.").ToUpperInvariant();
            var pfcKey = $"pfc:{code}:{version}";
            if (!categoryKeys.Add(pfcKey))
                throw new ArgumentException("Duplicate pfcPrimary mapping in request.");

            if (!validPfcKeys.Contains((code, version)))
                throw new ArgumentException($"Invalid pfcPrimary mapping '{code}' ({version}).");

            normalizedCategories.Add(new ProfileBudgetCategory
            {
                Id = Guid.NewGuid(),
                PfcPrimaryCode = code,
                PfcVersion = version
            });
        }

        var bankAccounts = new List<ProfileBudgetBankAccount>();
        var accountIds = new HashSet<Guid>();
        foreach (var linkedBankAccountId in request.LinkedBankAccountIds.Distinct())
        {
            if (!accountIds.Add(linkedBankAccountId))
                continue;

            var account = await linkedBankAccountRepository
                .GetByIdAsync(linkedBankAccountId, cancellationToken)
                .ConfigureAwait(false)
                ?? throw new KeyNotFoundException("Linked bank account was not found.");

            var bank = await linkedBankRepository
                .GetByIdForProfileAsync(account.LinkedBankId, profileId, cancellationToken)
                .ConfigureAwait(false);

            if (bank is null)
                throw new ArgumentException("Linked bank account does not belong to your profile.");

            bankAccounts.Add(new ProfileBudgetBankAccount
            {
                Id = Guid.NewGuid(),
                LinkedBankAccountId = linkedBankAccountId
            });
        }

        return new NormalizedCreateProfileBudget
        {
            Name = name,
            AmountLimit = request.AmountLimit,
            IsRecurring = request.IsRecurring,
            PeriodType = periodType,
            StartDate = startDate,
            EndDate = endDate,
            ProfileCustomCategorySetId = customCategorySetId,
            IncludeIncome = request.IncludeIncome,
            IncludeUnlinkedTransactions = request.IncludeUnlinkedTransactions,
            Categories = normalizedCategories,
            BankAccounts = bankAccounts
        };
    }

    private static ProfileBudgetCategoryResponse ToCategoryResponse(
        ProfileBudgetCategory category,
        IReadOnlyDictionary<Guid, ProfileCustomCategoryResponse> customCategoryResponsesById) => new()
    {
        Id = category.Id,
        PfcPrimaryCode = category.PfcPrimaryCode,
        PfcVersion = category.PfcVersion,
        CustomCategory = category.CustomCategoryId is { } customCategoryId &&
                         customCategoryResponsesById.TryGetValue(customCategoryId, out var customCategory)
            ? customCategory
            : null
    };

    private static ProfileBudgetPeriodResponse ToPeriodResponse(ProfileBudgetPeriod period) => new()
    {
        Id = period.Id,
        PeriodStartDate = period.PeriodStartDate,
        PeriodEndDate = period.PeriodEndDate,
        PeriodName = period.PeriodName,
        AmountLimit = period.AmountLimit,
        SpentAmount = period.SpentAmount,
        CreatedAt = period.CreatedAt,
        UpdatedAt = period.UpdatedAt
    };

    private static string TrimRequired(string? value, string message) =>
        string.IsNullOrWhiteSpace(value) ? throw new ArgumentException(message) : value.Trim();

    private static DateOnly ParseRequiredDate(string? raw, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(raw))
            throw new ArgumentException($"{fieldName} is required.");

        return DateOnly.TryParse(raw.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            ? parsed
            : throw new ArgumentException($"{fieldName} must be an ISO date (YYYY-MM-DD).");
    }

    private sealed class NormalizedCreateProfileBudget
    {
        public string Name { get; init; } = string.Empty;
        public decimal AmountLimit { get; init; }
        public bool IsRecurring { get; init; }
        public string? PeriodType { get; init; }
        public DateOnly StartDate { get; init; }
        public DateOnly? EndDate { get; init; }
        public Guid? ProfileCustomCategorySetId { get; init; }
        public bool IncludeIncome { get; init; }
        public bool IncludeUnlinkedTransactions { get; init; }
        public IReadOnlyList<ProfileBudgetCategory> Categories { get; init; } = [];
        public IReadOnlyList<ProfileBudgetBankAccount> BankAccounts { get; init; } = [];
    }
}
