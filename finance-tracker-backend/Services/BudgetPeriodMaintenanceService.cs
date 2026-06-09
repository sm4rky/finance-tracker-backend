using finance_tracker_backend.Infrastructure;
using finance_tracker_backend.Models;
using finance_tracker_backend.Repositories;

namespace finance_tracker_backend.Services;

public sealed class BudgetPeriodMaintenanceService(
    IProfileBudgetRepository budgetRepository,
    IProfileBudgetPeriodRepository budgetPeriodRepository,
    IBudgetPeriodRefreshService budgetPeriodRefreshService) : IBudgetPeriodMaintenanceService
{
    public async Task EnsureInitialPeriodsAsync(
        ProfileBudget budget,
        CancellationToken cancellationToken = default)
    {
        if (!budget.IsActive)
            return;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        if (budget.IsRecurring)
        {
            if (budget.StartDate > today)
                return;

            if (BudgetPeriodHelper.IsBudgetExpired(today, budget.EndDate))
            {
                await budgetRepository.DeactivateAsync(budget.Id, DateTimeOffset.UtcNow, cancellationToken)
                    .ConfigureAwait(false);
                return;
            }

            await EnsureRecurringPeriodForDateAsync(budget, today, cancellationToken)
                .ConfigureAwait(false);

            return;
        }

        await EnsureFixedPeriodAsync(budget, cancellationToken).ConfigureAwait(false);
    }

    public async Task RunDailyMaintenanceAsync(CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var recurringBudgets = await budgetRepository.ListActiveRecurringAsync(cancellationToken).ConfigureAwait(false);

        foreach (var budget in recurringBudgets)
        {
            if (BudgetPeriodHelper.IsBudgetExpired(today, budget.EndDate))
            {
                await budgetRepository.DeactivateAsync(budget.Id, DateTimeOffset.UtcNow, cancellationToken)
                    .ConfigureAwait(false);
                continue;
            }

            if (budget.StartDate > today)
                continue;

            await EnsureRecurringPeriodForDateAsync(budget, today, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private async Task EnsureFixedPeriodAsync(ProfileBudget budget, CancellationToken cancellationToken)
    {
        var existing = await budgetPeriodRepository
            .GetByBudgetAndRangeAsync(budget.Id, budget.StartDate, budget.EndDate!.Value, cancellationToken)
            .ConfigureAwait(false);
        if (existing is not null)
            return;

        var now = DateTimeOffset.UtcNow;
        var period = new ProfileBudgetPeriod
        {
            Id = Guid.NewGuid(),
            BudgetId = budget.Id,
            PeriodStartDate = budget.StartDate,
            PeriodEndDate = budget.EndDate!.Value,
            PeriodName = BudgetPeriodHelper.FormatFixedPeriodName(budget.StartDate, budget.EndDate.Value),
            AmountLimit = budget.AmountLimit,
            SpentAmount = 0m,
            CreatedAt = now,
            UpdatedAt = now
        };

        await budgetPeriodRepository.InsertAsync(period, cancellationToken).ConfigureAwait(false);
        await budgetPeriodRefreshService.RefreshPeriodAsync(period.Id, cancellationToken).ConfigureAwait(false);
    }

    private async Task<ProfileBudgetPeriod?> EnsureRecurringPeriodForDateAsync(
        ProfileBudget budget,
        DateOnly date,
        CancellationToken cancellationToken)
    {
        var (periodStart, periodEnd) = BudgetPeriodHelper.GetCalendarPeriodForDate(budget.PeriodType!, date);

        if (budget.EndDate is { } endDate && periodStart > endDate)
            return null;

        var existing = await budgetPeriodRepository
            .GetByBudgetAndRangeAsync(budget.Id, periodStart, periodEnd, cancellationToken)
            .ConfigureAwait(false);
        if (existing is not null)
        {
            await budgetPeriodRefreshService.RefreshPeriodAsync(existing.Id, cancellationToken)
                .ConfigureAwait(false);
            return existing;
        }

        var now = DateTimeOffset.UtcNow;
        var period = new ProfileBudgetPeriod
        {
            Id = Guid.NewGuid(),
            BudgetId = budget.Id,
            PeriodStartDate = periodStart,
            PeriodEndDate = periodEnd,
            PeriodName = BudgetPeriodHelper.FormatPeriodName(budget.PeriodType!, periodStart, periodEnd),
            AmountLimit = budget.AmountLimit,
            SpentAmount = 0m,
            CreatedAt = now,
            UpdatedAt = now
        };

        await budgetPeriodRepository.InsertAsync(period, cancellationToken).ConfigureAwait(false);
        await budgetPeriodRefreshService.RefreshPeriodAsync(period.Id, cancellationToken).ConfigureAwait(false);
        return period;
    }
}
