using finance_tracker_backend.Infrastructure;
using finance_tracker_backend.Models;
using finance_tracker_backend.Repositories;

namespace finance_tracker_backend.Services;

public sealed class BudgetPeriodRefreshService(
    IProfileBudgetRepository budgetRepository,
    IProfileBudgetCategoryRepository budgetCategoryRepository,
    IProfileBudgetBankAccountRepository budgetBankAccountRepository,
    IProfileBudgetPeriodRepository budgetPeriodRepository,
    ITransactionRepository transactionRepository,
    CustomCategorySetHelper customCategorySetHelper,
    IBudgetAlertNotificationService budgetAlertNotificationService) : IBudgetPeriodRefreshService
{
    public async Task RefreshForProfileDatesAsync(
        Guid profileId,
        IReadOnlyCollection<DateOnly> dates,
        CancellationToken cancellationToken = default)
    {
        if (dates.Count == 0)
            return;

        var periods = await budgetPeriodRepository
            .ListActiveBudgetPeriodsContainingAnyDateAsync(profileId, dates, cancellationToken)
            .ConfigureAwait(false);

        if (periods.Count == 0)
            return;

        var budgetIds = periods.Select(p => p.BudgetId).Distinct().ToList();
        var budgets = (await budgetRepository.ListByProfileIdAsync(profileId, cancellationToken).ConfigureAwait(false))
            .Where(b => budgetIds.Contains(b.Id))
            .ToDictionary(b => b.Id);
        var categoriesByBudget = (await budgetCategoryRepository
                .ListByBudgetIdsAsync(budgetIds, cancellationToken)
                .ConfigureAwait(false))
            .GroupBy(c => c.BudgetId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<ProfileBudgetCategory>)g.ToList());
        var accountsByBudget = (await budgetBankAccountRepository
                .ListByBudgetIdsAsync(budgetIds, cancellationToken)
                .ConfigureAwait(false))
            .GroupBy(a => a.BudgetId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<ProfileBudgetBankAccount>)g.ToList());

        foreach (var period in periods)
        {
            if (!budgets.TryGetValue(period.BudgetId, out var budget) || !budget.IsActive)
                continue;

            var categories = categoriesByBudget.GetValueOrDefault(period.BudgetId) ?? [];
            var accounts = accountsByBudget.GetValueOrDefault(period.BudgetId) ?? [];
            await RefreshPeriodAsync(budget, period, categories, accounts, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task RefreshPeriodAsync(Guid periodId, CancellationToken cancellationToken = default)
    {
        var period = await budgetPeriodRepository.GetByIdAsync(periodId, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException("Budget period was not found.");

        var budget = await budgetRepository.GetByIdAsync(period.BudgetId, cancellationToken).ConfigureAwait(false);
        if (budget is null || !budget.IsActive)
            return;

        var categories = await budgetCategoryRepository.ListByBudgetIdAsync(budget.Id, cancellationToken)
            .ConfigureAwait(false);
        var accounts = await budgetBankAccountRepository.ListByBudgetIdAsync(budget.Id, cancellationToken)
            .ConfigureAwait(false);

        await RefreshPeriodAsync(budget, period, categories, accounts, cancellationToken).ConfigureAwait(false);
    }

    private async Task RefreshPeriodAsync(
        ProfileBudget budget,
        ProfileBudgetPeriod period,
        IReadOnlyList<ProfileBudgetCategory> categories,
        IReadOnlyList<ProfileBudgetBankAccount> accounts,
        CancellationToken cancellationToken)
    {
        var (effectiveFrom, effectiveTo) = BudgetPeriodHelper.GetEffectiveTransactionRange(
            period.PeriodStartDate,
            period.PeriodEndDate,
            budget.IsRecurring,
            budget.StartDate,
            budget.EndDate);

        var pfcPrimaryList = await ResolvePfcPrimaryListAsync(budget, categories, cancellationToken)
            .ConfigureAwait(false);
        var accountIds = accounts.Select(account => account.LinkedBankAccountId).ToList();
        var query = TransactionsQueryHelper.CreateForBudgetPeriod(
            accountIds,
            budget.IncludeUnlinkedTransactions,
            pfcPrimaryList,
            effectiveFrom,
            effectiveTo,
            budget.IncludeIncome);

        var spentAmount = await transactionRepository
            .SumBudgetSpentAmountAsync(budget.ProfileId, query, cancellationToken)
            .ConfigureAwait(false);

        if (spentAmount < 0m)
            spentAmount = 0m;

        await budgetPeriodRepository
            .UpdateSpentAmountAsync(period.Id, spentAmount, DateTimeOffset.UtcNow, cancellationToken)
            .ConfigureAwait(false);

        await budgetAlertNotificationService
            .SendBudgetAlertAsync(budget, period, spentAmount, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<string>> ResolvePfcPrimaryListAsync(
        ProfileBudget budget,
        IReadOnlyList<ProfileBudgetCategory> categories,
        CancellationToken cancellationToken)
    {
        var codes = new HashSet<string>(StringComparer.Ordinal);

        foreach (var category in categories)
        {
            if (!string.IsNullOrWhiteSpace(category.PfcPrimaryCode))
                codes.Add(category.PfcPrimaryCode);
        }

        var customCategoryIds = categories
            .Where(category => category.CustomCategoryId is not null)
            .Select(category => category.CustomCategoryId!.Value)
            .ToList();

        if (customCategoryIds.Count == 0 || budget.ProfileCustomCategorySetId is not { } customCategorySetId)
            return codes.ToList();

        var customCategoryByPfcPrimary = await customCategorySetHelper
            .LoadCustomCategoryByPfcPrimaryAsync(
                budget.ProfileId,
                customCategorySetId,
                customCategoryIds,
                cancellationToken)
            .ConfigureAwait(false);

        if (customCategoryByPfcPrimary is null)
            return codes.ToList();

        foreach (var pfcPrimary in customCategoryByPfcPrimary.Keys)
        {
            if (!string.IsNullOrWhiteSpace(pfcPrimary.PfcPrimaryCode))
                codes.Add(pfcPrimary.PfcPrimaryCode);
        }

        return codes.ToList();
    }
}
