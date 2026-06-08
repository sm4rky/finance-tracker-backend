using System.Globalization;

namespace finance_tracker_backend.Infrastructure;

public static class BudgetPeriodHelper
{
    private const string Weekly = "WEEKLY";
    private const string Monthly = "MONTHLY";
    private const string Yearly = "YEARLY";

    private static readonly CultureInfo EnUs = CultureInfo.GetCultureInfo("en-US");

    private static readonly HashSet<string> PeriodTypes = new(StringComparer.Ordinal)
    {
        Weekly,
        Monthly,
        Yearly
    };

    public static string ParsePeriodType(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            throw new ArgumentException("periodType is required.");

        var normalized = raw.Trim().ToUpperInvariant();
        return !PeriodTypes.Contains(normalized) ? throw new ArgumentException("periodType must be 'WEEKLY', 'MONTHLY', or 'YEARLY'.") : normalized;
    }

    public static (DateOnly Start, DateOnly End) GetCalendarPeriodForDate(string periodType, DateOnly date) =>
        periodType switch
        {
            Weekly => GetWeekPeriod(date),
            Monthly => GetMonthPeriod(date),
            Yearly => GetYearPeriod(date),
            _ => throw new ArgumentException("periodType must be 'WEEKLY', 'MONTHLY', or 'YEARLY'.")
        };

    public static (DateOnly EffectiveFrom, DateOnly EffectiveTo) GetEffectiveTransactionRange(
        DateOnly periodStartDate,
        DateOnly periodEndDate,
        bool isRecurring,
        DateOnly budgetStartDate,
        DateOnly? budgetEndDate)
    {
        var effectiveFrom = isRecurring
            ? periodStartDate
            : periodStartDate > budgetStartDate ? periodStartDate : budgetStartDate;
        var effectiveTo = budgetEndDate is { } endDate && endDate < periodEndDate
            ? endDate
            : periodEndDate;

        return (effectiveFrom, effectiveTo);
    }

    public static bool ShouldTrackBudgetOnDate(
        DateOnly date,
        DateOnly budgetStartDate,
        DateOnly? budgetEndDate) =>
        date >= budgetStartDate && (budgetEndDate is null || date <= budgetEndDate);

    public static bool IsBudgetExpired(DateOnly today, DateOnly? budgetEndDate) =>
        budgetEndDate is { } endDate && today > endDate;

    public static string FormatPeriodName(string periodType, DateOnly start, DateOnly end) =>
        periodType switch
        {
            Monthly => start.ToString("MMMM yyyy", EnUs),
            Yearly => start.Year.ToString(CultureInfo.InvariantCulture),
            Weekly when start.Year == end.Year =>
                $"{start.ToString("MMM d", EnUs)} - {end.ToString("MMM d", EnUs)}, {end.Year}",
            Weekly =>
                $"{start.ToString("MMM d, yyyy", EnUs)} - {end.ToString("MMM d, yyyy", EnUs)}",
            _ => $"{start:yyyy-MM-dd} - {end:yyyy-MM-dd}"
        };

    public static string FormatFixedPeriodName(DateOnly start, DateOnly end) =>
        start.Year == end.Year
            ? $"{start.ToString("MMM d", EnUs)} - {end.ToString("MMM d, yyyy", EnUs)}"
            : $"{start.ToString("MMM d, yyyy", EnUs)} - {end.ToString("MMM d, yyyy", EnUs)}";

    private static (DateOnly Start, DateOnly End) GetWeekPeriod(DateOnly date)
    {
        var dayOfWeek = date.DayOfWeek;
        var daysFromMonday = ((int)dayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        var start = date.AddDays(-daysFromMonday);
        return (start, start.AddDays(6));
    }

    private static (DateOnly Start, DateOnly End) GetMonthPeriod(DateOnly date)
    {
        var start = new DateOnly(date.Year, date.Month, 1);
        return (start, start.AddMonths(1).AddDays(-1));
    }

    private static (DateOnly Start, DateOnly End) GetYearPeriod(DateOnly date)
    {
        var start = new DateOnly(date.Year, 1, 1);
        return (start, new DateOnly(date.Year, 12, 31));
    }
}
