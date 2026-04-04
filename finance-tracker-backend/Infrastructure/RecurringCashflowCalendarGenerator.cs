using finance_tracker_backend.Contracts.Responses;
using finance_tracker_backend.Models;

namespace finance_tracker_backend.Infrastructure;

/// <summary>Projects recurring cashflow rows into calendar occurrences for a date range (not persisted).</summary>
/// <remarks>SEMI_MONTHLY MVP: see <see cref="ExpandSemiMonthly"/>.</remarks>
public static class RecurringCashflowCalendarGenerator
{
    public static IReadOnlyList<ProfileRecurringCashflowCalendarOccurrenceResponse> ExpandRow(
        ProfileRecurringCashflow row,
        DateOnly rangeStart,
        DateOnly rangeEnd,
        RecurringCashflowLinkedBankAccountResponse? linkedAccount)
    {
        if (row.Status == "inactive")
            return [];

        var amount = row.ExpectedAmount;
        if (amount == 0 && row.LastAmount is { } la)
            amount = la;
        if (amount == 0)
            return [];

        var anchor = ResolveAnchor(row);
        if (anchor is null)
            return [];

        var dates = row.Frequency switch
        {
            "ONE_TIME" => ExpandOneTime(anchor.Value, rangeStart, rangeEnd),
            "WEEKLY" => ExpandWeekly(anchor.Value, rangeStart, rangeEnd),
            "BIWEEKLY" => ExpandBiweekly(anchor.Value, rangeStart, rangeEnd),
            "MONTHLY" => ExpandMonthly(anchor.Value, rangeStart, rangeEnd),
            "ANNUALLY" => ExpandAnnually(anchor.Value, rangeStart, rangeEnd),
            "SEMI_MONTHLY" => ExpandSemiMonthly(anchor.Value, rangeStart, rangeEnd),
            "UNKNOWN" => [],
            _ => []
        };

        var list = new List<ProfileRecurringCashflowCalendarOccurrenceResponse>(dates.Count);
        list.AddRange(dates.Select(d => new ProfileRecurringCashflowCalendarOccurrenceResponse
        {
            Date = d,
            Amount = amount,
            MerchantName = row.MerchantName,
            Description = row.Description,
            PfcPrimary = row.PfcPrimary,
            PfcDetailed = row.PfcDetailed,
            RecurringCashflowId = row.Id,
            Frequency = row.Frequency,
            Direction = row.Direction,
            Status = row.Status,
            LinkedBankAccount = linkedAccount
        }));

        return list;
    }

    private static DateOnly? ResolveAnchor(ProfileRecurringCashflow row)
    {
        if (row.PredictedNextDate is { } p)
            return p;
        if (row.LastDate is { } l)
            return l;
        if (row.FirstDate is { } f)
            return f;
        return null;
    }

    private static IReadOnlyList<DateOnly> ExpandOneTime(DateOnly anchor, DateOnly from, DateOnly to)
    {
        if (anchor >= from && anchor <= to)
            return [anchor];
        return [];
    }

    private static IReadOnlyList<DateOnly> ExpandWeekly(DateOnly anchor, DateOnly from, DateOnly to)
    {
        var d = anchor;
        while (d < from)
            d = d.AddDays(7);

        var list = new List<DateOnly>();
        while (d <= to)
        {
            list.Add(d);
            d = d.AddDays(7);
        }

        return list;
    }

    private static IReadOnlyList<DateOnly> ExpandBiweekly(DateOnly anchor, DateOnly from, DateOnly to)
    {
        var d = anchor;
        while (d < from)
            d = d.AddDays(14);

        var list = new List<DateOnly>();
        while (d <= to)
        {
            list.Add(d);
            d = d.AddDays(14);
        }

        return list;
    }

    private static IReadOnlyList<DateOnly> ExpandMonthly(DateOnly anchor, DateOnly from, DateOnly to)
    {
        var list = new List<DateOnly>();
        var cursor = new DateOnly(from.Year, from.Month, 1);
        var endMonth = new DateOnly(to.Year, to.Month, 1);

        while (cursor <= endMonth)
        {
            var dim = DateTime.DaysInMonth(cursor.Year, cursor.Month);
            var day = Math.Min(anchor.Day, dim);
            var date = new DateOnly(cursor.Year, cursor.Month, day);
            if (date >= from && date <= to)
                list.Add(date);
            cursor = cursor.AddMonths(1);
        }

        return list;
    }

    private static IReadOnlyList<DateOnly> ExpandAnnually(DateOnly anchor, DateOnly from, DateOnly to)
    {
        var list = new List<DateOnly>();
        for (var y = from.Year; y <= to.Year; y++)
        {
            var dim = DateTime.DaysInMonth(y, anchor.Month);
            var day = Math.Min(anchor.Day, dim);
            var date = new DateOnly(y, anchor.Month, day);
            if (date >= from && date <= to)
                list.Add(date);
        }

        return list;
    }
    
    private static IReadOnlyList<DateOnly> ExpandSemiMonthly(DateOnly anchor, DateOnly from, DateOnly to)
    {
        var set = new SortedSet<DateOnly>();
        var cursor = new DateOnly(from.Year, from.Month, 1);
        var endMonth = new DateOnly(to.Year, to.Month, 1);

        while (cursor <= endMonth)
        {
            var dim = DateTime.DaysInMonth(cursor.Year, cursor.Month);
            var day = Math.Min(anchor.Day, dim);
            var first = new DateOnly(cursor.Year, cursor.Month, day);
            var second = first.AddDays(15);

            if (first >= from && first <= to)
                set.Add(first);
            if (second >= from && second <= to)
                set.Add(second);

            cursor = cursor.AddMonths(1);
        }

        return set.ToList();
    }
}
