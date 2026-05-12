using finance_tracker_backend.Models;

namespace finance_tracker_backend.Infrastructure;

/// <summary>Computes <c>last_date</c> and the next <c>predicted_next_date</c> after one occurrence on <paramref name="occurredOn"/>.</summary>
public static class RecurringCashflowPredictedDateAdvancement
{
    public static bool TryAdvance(
        ProfileRecurringCashflow row,
        DateOnly occurredOn,
        out DateOnly newLastDate,
        out DateOnly? newPredictedNextDate)
    {
        newLastDate = occurredOn;
        newPredictedNextDate = null;

        var frequency = row.Frequency.Trim().ToUpperInvariant();
        return frequency switch
        {
            "ONE_TIME" => true,
            "WEEKLY" => SetNext(ref newPredictedNextDate, occurredOn.AddDays(7)),
            "BIWEEKLY" => SetNext(ref newPredictedNextDate, occurredOn.AddDays(14)),
            "MONTHLY" => SetNext(ref newPredictedNextDate, AddMonthsClamped(occurredOn, 1)),
            "ANNUALLY" => SetNext(ref newPredictedNextDate, AddYearsClamped(occurredOn, 1)),
            "SEMI_MONTHLY" => SetNext(
                ref newPredictedNextDate,
                NextSemiMonthlyAfter(occurredOn, ResolveSemiMonthlyAnchorDay(row, occurredOn))),
            _ => false
        };
    }

    private static bool SetNext(ref DateOnly? target, DateOnly value)
    {
        target = value;
        return true;
    }

    private static int ResolveSemiMonthlyAnchorDay(ProfileRecurringCashflow row, DateOnly occurredOn)
    {
        if (row.FirstDate is { } f)
            return f.Day;
        if (row.LastDate is { } l)
            return l.Day;
        return occurredOn.Day;
    }

    private static DateOnly NextSemiMonthlyAfter(DateOnly occurred, int anchorDay)
    {
        var y = occurred.Year;
        var m = occurred.Month;
        for (var i = 0; i < 48; i++)
        {
            var dim = DateTime.DaysInMonth(y, m);
            var day = Math.Min(anchorDay, dim);
            var first = new DateOnly(y, m, day);
            var second = first.AddDays(15);
            if (first > occurred)
                return first;
            if (second > occurred)
                return second;

            var nextMonth = new DateOnly(y, m, 1).AddMonths(1);
            y = nextMonth.Year;
            m = nextMonth.Month;
        }

        throw new InvalidOperationException("SEMI_MONTHLY: could not find a next occurrence within 48 months.");
    }

    private static DateOnly AddMonthsClamped(DateOnly d, int months)
    {
        var next = d.AddMonths(months);
        var dim = DateTime.DaysInMonth(next.Year, next.Month);
        var day = Math.Min(d.Day, dim);
        return new DateOnly(next.Year, next.Month, day);
    }

    private static DateOnly AddYearsClamped(DateOnly d, int years)
    {
        var y = d.Year + years;
        var dim = DateTime.DaysInMonth(y, d.Month);
        var day = Math.Min(d.Day, dim);
        return new DateOnly(y, d.Month, day);
    }
}
