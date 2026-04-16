using System.Globalization;

namespace finance_tracker_backend.Infrastructure;

public static class AnalyticsTimeGranularityHelper
{
    private const string Day = "day";
    private const string Week = "week";
    private const string Month = "month";
    private const string Year = "year";

    private static readonly CultureInfo EnUs = CultureInfo.GetCultureInfo("en-US");

    private static bool TryParse(string? raw, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        var n = raw.Trim().ToLowerInvariant() switch
        {
            Day => Day,
            Week => Week,
            Month => Month,
            Year => Year,
            _ => null
        };

        if (n is null)
            return false;

        normalized = n;
        return true;
    }

    public static string Parse(string? raw)
    {
        if (!TryParse(raw, out var normalized))
            throw new ArgumentException(
                string.IsNullOrWhiteSpace(raw)
                    ? "timeGranularity is required."
                    : "timeGranularity must be day, week, month, or year.");

        return normalized;
    }

    public static bool MonthBucketsSpanMultipleYears(IReadOnlyList<DateOnly> periodStartDates) =>
        periodStartDates.Count > 0 &&
        periodStartDates.Select(d => d.Year).Distinct().Count() > 1;

    public static bool DateRangeSpansMultipleYears(DateOnly dateFromInclusive, DateOnly dateToInclusive) =>
        dateFromInclusive.Year != dateToInclusive.Year;

    public static string FormatPeriodLabel(
        DateOnly periodStartDate,
        string timeGranularity,
        bool monthBucketsSpanMultipleYears,
        bool dateRangeSpansMultipleYears)
    {
        return timeGranularity switch
        {
            Year => periodStartDate.ToString("yyyy", EnUs),
            Month => monthBucketsSpanMultipleYears
                ? periodStartDate.ToString("MMM yyyy", EnUs)
                : periodStartDate.ToString("MMM", EnUs),
            Day or Week => FormatDayOrWeekLabel(periodStartDate, dateRangeSpansMultipleYears),
            _ => throw new ArgumentOutOfRangeException(nameof(timeGranularity), timeGranularity, null)
        };
    }

    private static string FormatDayOrWeekLabel(DateOnly d, bool includeYear)
    {
        var monthDay = $"{d.ToString("MMM", EnUs)} {DayWithOrdinal(d.Day)}";
        return includeYear ? $"{monthDay}, {d.Year}" : monthDay;
    }

    private static string DayWithOrdinal(int day)
    {
        var suffix = (day % 100) switch
        {
            11 or 12 or 13 => "th",
            _ => (day % 10) switch
            {
                1 => "st",
                2 => "nd",
                3 => "rd",
                _ => "th"
            }
        };
        return $"{day}{suffix}";
    }
}
