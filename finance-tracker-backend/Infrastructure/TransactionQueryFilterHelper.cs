using System.Globalization;
using finance_tracker_backend.Contracts.Requests;
using finance_tracker_backend.Enums;
using finance_tracker_backend.Types;

namespace finance_tracker_backend.Infrastructure;

public static class TransactionQueryFilterHelper
{
    public const string PfcPrimaryUncategorizedQueryValue = "__UNCATEGORIZED__";

    public static TransactionQueryFilters CreateForPagedQuery(
        QueryTransactionsRequest request,
        int page,
        int pageSize,
        TransactionSortField sortBy,
        bool descending)
    {
        var core = BuildCore(
            request.AccountIds,
            request.IncludeUnlinkedTransactions,
            request.PfcPrimaryList,
            request.PaymentChannels,
            request.Pending,
            request.DateFrom,
            request.DateTo,
            request.AmountMin,
            request.AmountMax,
            request.AmountFlow);

        return core with
        {
            Offset = (page - 1) * pageSize,
            Limit = pageSize,
            SortBy = sortBy,
            Descending = descending
        };
    }

    public static TransactionQueryFilters CreateForCashflowAggregation(QueryCashflowRequest request)
    {
        return BuildCore(
            request.AccountIds,
            request.IncludeUnlinkedTransactions,
            request.PfcPrimaryList,
            request.PaymentChannels,
            request.Pending,
            request.DateFrom,
            request.DateTo,
            request.AmountMin,
            request.AmountMax,
            request.AmountFlow);
    }

    public static TransactionFlow ParseAndValidateAmountFlow(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            throw new ArgumentException("amountFlow is required.");

        return ParseAmountFlowValue(raw);
    }

    private static TransactionFlow ParseAmountFlowValue(string raw)
    {
        var value = raw.Trim();

        if (value.Equals("income", StringComparison.OrdinalIgnoreCase))
            return TransactionFlow.Income;

        if (value.Equals("expense", StringComparison.OrdinalIgnoreCase))
            return TransactionFlow.Expense;

        throw new ArgumentException("amountFlow must be 'income' or 'expense'.");
    }

    private static TransactionQueryFilters BuildCore(
        List<Guid>? accountIds,
        bool? includeUnlinkedTransactions,
        List<string>? pfcPrimaryList,
        List<string>? paymentChannels,
        bool? pending,
        string? dateFrom,
        string? dateTo,
        decimal? amountMin,
        decimal? amountMax,
        string? amountFlow)
    {
        IReadOnlyList<Guid> resolvedAccountIds = accountIds is { Count: > 0 }
            ? accountIds.Distinct().ToList()
            : Array.Empty<Guid>();

        var includePfcUncategorized = false;
        var pfcList = new List<string>();

        foreach (var value in from rawValue in pfcPrimaryList ?? []
                 where !string.IsNullOrWhiteSpace(rawValue)
                 select rawValue.Trim())
        {
            if (string.Equals(value, PfcPrimaryUncategorizedQueryValue, StringComparison.Ordinal))
            {
                includePfcUncategorized = true;
                continue;
            }

            pfcList.Add(value);
        }

        var distinctPfcPrimaryList = pfcList
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var paymentChannelList = (paymentChannels ?? [])
            .Where(channel => !string.IsNullOrWhiteSpace(channel))
            .Select(channel => channel.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();

        DateOnly? dateFromInclusive = null;
        DateOnly? dateToInclusive = null;

        if (!string.IsNullOrWhiteSpace(dateFrom))
        {
            if (!DateOnly.TryParse(
                    dateFrom.Trim(),
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var parsedDateFrom))
            {
                throw new ArgumentException("dateFrom must be an ISO date (YYYY-MM-DD).");
            }

            dateFromInclusive = parsedDateFrom;
        }

        if (!string.IsNullOrWhiteSpace(dateTo))
        {
            if (!DateOnly.TryParse(
                    dateTo.Trim(),
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var parsedDateTo))
            {
                throw new ArgumentException("dateTo must be an ISO date (YYYY-MM-DD).");
            }

            dateToInclusive = parsedDateTo;
        }

        var todayUtc = DateOnly.FromDateTime(DateTime.UtcNow);

        if (dateFromInclusive is { } from && from > todayUtc)
            throw new ArgumentException("dateFrom cannot be in the future.");

        if (dateToInclusive is { } to && to > todayUtc)
            throw new ArgumentException("dateTo cannot be in the future.");

        if (amountMin is < 0)
            throw new ArgumentException("amountMin must be non-negative.");

        if (amountMax is < 0)
            throw new ArgumentException("amountMax must be non-negative.");

        if (amountMin is { } min && amountMax is { } max && min > max)
            throw new ArgumentException("amountMin must be less than or equal to amountMax.");

        TransactionFlow? resolvedAmountFlow = null;
        if (!string.IsNullOrWhiteSpace(amountFlow))
            resolvedAmountFlow = ParseAmountFlowValue(amountFlow);

        return new TransactionQueryFilters
        {
            Offset = 0,
            Limit = 1,
            SortBy = TransactionSortField.Date,
            Descending = true,
            AccountIds = resolvedAccountIds,
            IncludeUnlinkedTransactions = includeUnlinkedTransactions ?? true,
            PfcPrimaryList = distinctPfcPrimaryList,
            IncludePfcUncategorized = includePfcUncategorized,
            PaymentChannels = paymentChannelList,
            Pending = pending,
            DateFromInclusive = dateFromInclusive,
            DateToInclusive = dateToInclusive,
            AbsAmountMin = amountMin,
            AbsAmountMax = amountMax,
            AmountFlow = resolvedAmountFlow
        };
    }
}
