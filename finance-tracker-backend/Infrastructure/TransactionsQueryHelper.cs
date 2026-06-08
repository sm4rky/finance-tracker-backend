using System.Globalization;
using finance_tracker_backend.Contracts.Requests;
using finance_tracker_backend.Enums;
using finance_tracker_backend.Types;

namespace finance_tracker_backend.Infrastructure;

public static class TransactionsQueryHelper
{
    public const string PfcPrimaryUncategorizedQueryValue = "__UNCATEGORIZED__";

    private static readonly Dictionary<string, TransactionSortField> SortByDictionary =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["merchantName"] = TransactionSortField.MerchantName,
            ["linkedBankAccountId"] = TransactionSortField.LinkedBankAccountId,
            ["pfcPrimary"] = TransactionSortField.PfcPrimary,
            ["pfcDetailed"] = TransactionSortField.PfcDetailed,
            ["date"] = TransactionSortField.Date,
            ["amount"] = TransactionSortField.Amount,
            ["paymentChannel"] = TransactionSortField.PaymentChannel,
            ["pending"] = TransactionSortField.Pending
        };

    public static TransactionsQuery CreateForPagedTransactionsQuery(QueryTransactionsRequest request)
    {
        return new TransactionsQuery
        {
            Offset = (request.Page!.Value - 1) * request.Limit!.Value,
            Limit = request.Limit!.Value,
            SortBy = ParseSortBy(request.SortBy),
            Descending = ParseSortDirection(request.SortDirection),
            AccountIds = request.AccountIds?.Distinct().ToList() ?? [],
            IncludeUnlinkedTransactions = request.IncludeUnlinkedTransactions ?? true,
            PfcPrimaryList = request.PfcPrimaryList ?? [],
            IncludePfcUncategorized = IncludeUncategorized(request.PfcPrimaryList),
            PaymentChannels = request.PaymentChannels ?? [],
            Pending = request.Pending,
            DateFromInclusive = ParseDate(request.DateFrom, "dateFrom"),
            DateToInclusive = ParseDate(request.DateTo, "dateTo"),
            AbsAmountMin = request.AmountMin,
            AbsAmountMax = request.AmountMax,
            AmountFlow = string.IsNullOrWhiteSpace(request.AmountFlow) ? null : ParseTransactionFlow(request.AmountFlow)
        };
    }

    public static TransactionsQuery CreateForAnalyticsAggregation(TransactionAnalyticsQueryRequest request) => new()
    {
        Offset = 0,
        Limit = 1,
        SortBy = TransactionSortField.Date,
        Descending = true,
        AccountIds = request.AccountIds?.Distinct().ToList() ?? [],
        IncludeUnlinkedTransactions = request.IncludeUnlinkedTransactions ?? true,
        PfcPrimaryList = request.PfcPrimaryList ?? [],
        IncludePfcUncategorized = IncludeUncategorized(request.PfcPrimaryList),
        PaymentChannels = request.PaymentChannels ?? [],
        Pending = request.Pending,
        DateFromInclusive = ParseDate(request.DateFrom, "dateFrom"),
        DateToInclusive = ParseDate(request.DateTo, "dateTo"),
        AbsAmountMin = request.AmountMin,
        AbsAmountMax = request.AmountMax,
        AmountFlow = string.IsNullOrWhiteSpace(request.AmountFlow) ? null : ParseTransactionFlow(request.AmountFlow)
    };

    public static TransactionsQuery CreateForBudgetPeriod(
        IReadOnlyList<Guid> accountIds,
        bool includeUnlinkedTransactions,
        IReadOnlyList<string> pfcPrimaryList,
        DateOnly periodStartDate,
        DateOnly periodEndDate,
        bool includeIncome)
    {
        return new TransactionsQuery
        {
            Offset = 0,
            Limit = 1,
            SortBy = TransactionSortField.Date,
            Descending = true,
            AccountIds = accountIds.Distinct().ToList(),
            IncludeUnlinkedTransactions = includeUnlinkedTransactions,
            PfcPrimaryList = pfcPrimaryList.Distinct(StringComparer.Ordinal).ToList(),
            IncludeAllPaymentChannels = true,
            Pending = false,
            DateFromInclusive = periodStartDate,
            DateToInclusive = periodEndDate,
            AmountFlow = includeIncome ? null : TransactionFlow.Expense
        };
    }

    public static TransactionFlow ParseTransactionFlow(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) throw new ArgumentException("amountFlow is required.");

        var value = raw.Trim();

        if (value.Equals("income", StringComparison.OrdinalIgnoreCase))
            return TransactionFlow.Income;

        if (value.Equals("expense", StringComparison.OrdinalIgnoreCase))
            return TransactionFlow.Expense;

        throw new ArgumentException("amountFlow must be 'income' or 'expense'.");
    }

    private static TransactionSortField ParseSortBy(string? sortBy)
    {
        return string.IsNullOrWhiteSpace(sortBy)
            ? TransactionSortField.Date
            : SortByDictionary.TryGetValue(sortBy.Trim(), out var field)
                ? field
                : throw new ArgumentException(
                    $"Invalid sortBy '{sortBy}'. Allowed values: {string.Join(", ", SortByDictionary.Keys.Order(StringComparer.Ordinal))}.");
    }

    private static bool ParseSortDirection(string? sortDirection)
    {
        return sortDirection?.Trim().ToLowerInvariant() switch
        {
            null or "" or "desc" => true,
            "asc" => false,
            _ => throw new ArgumentException("sortDirection must be 'asc' or 'desc'.")
        };
    }

    private static bool IncludeUncategorized(IReadOnlyList<string>? values)
    {
        if (values == null) return false;
        var result = false;

        foreach (var value in values)
        {
            if (value != PfcPrimaryUncategorizedQueryValue) continue;
            result = true;
        }

        return result;
    }

    private static DateOnly? ParseDate(string? raw, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        return DateOnly.TryParse(
            raw.Trim(),
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var parsed)
            ? parsed
            : throw new ArgumentException($"{fieldName} must be an ISO date (YYYY-MM-DD).");
    }
}