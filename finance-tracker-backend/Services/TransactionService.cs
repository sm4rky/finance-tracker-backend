using System.Globalization;
using System.Security.Claims;
using finance_tracker_backend.Contracts.Pagination;
using finance_tracker_backend.Contracts.Requests;
using finance_tracker_backend.Contracts.Responses;
using finance_tracker_backend.Enums;
using finance_tracker_backend.Infrastructure;
using finance_tracker_backend.Models;
using finance_tracker_backend.Repositories;
using finance_tracker_backend.Types;

namespace finance_tracker_backend.Services;

public sealed class TransactionService(ITransactionRepository transactionRepository) : ITransactionService
{
    private const string PfcPrimaryUncategorizedQueryValue = "__UNCATEGORIZED__";

    public async Task<PagedResponse<TransactionResponse>> QueryAsync(
        ClaimsPrincipal user,
        QueryTransactionsRequest request,
        CancellationToken cancellationToken = default)
    {
        var profileId = user.RequireProfileId();

        if (request.Page is null or < 1)
            throw new ArgumentException("page is required and must be at least 1.");

        if (request.Limit is null or < 1 or > 100)
            throw new ArgumentException("limit is required and must be between 1 and 100.");

        var page = request.Page.Value;
        var pageSize = request.Limit.Value;

        var sortBy = string.IsNullOrWhiteSpace(request.SortBy)
            ? TransactionSortField.Date
            : ParseSortByFromQuery(request.SortBy);

        var descending = string.IsNullOrWhiteSpace(request.SortDirection) || ParseSortDirection(request.SortDirection.Trim());

        var query = BuildTransactionQuery(request, page, pageSize, sortBy, descending);

        var totalCount = await transactionRepository
            .CountAsync(profileId, query, cancellationToken)
            .ConfigureAwait(false);

        var rows = await transactionRepository
            .QueryPagedAsync(profileId, query, cancellationToken)
            .ConfigureAwait(false);

        return new PagedResponse<TransactionResponse>
        {
            Items = rows.Select(ToResponse).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    private static TransactionQueryFilters BuildTransactionQuery(
        QueryTransactionsRequest request,
        int page,
        int pageSize,
        TransactionSortField sortBy,
        bool descending)
    {
        IReadOnlyList<Guid> accountIds = request.AccountIds is { Count: > 0 }
            ? request.AccountIds
                .Distinct()
                .ToList()
            : Array.Empty<Guid>();

        var includePfcUncategorized = false;
        var pfcPrimaryList = new List<string>();

        foreach (var value in from rawValue in request.PfcPrimaryList ?? [] where !string.IsNullOrWhiteSpace(rawValue) select rawValue.Trim())
        {
            if (string.Equals(value, PfcPrimaryUncategorizedQueryValue, StringComparison.Ordinal))
            {
                includePfcUncategorized = true;
                continue;
            }

            pfcPrimaryList.Add(value);
        }

        var distinctPfcPrimaryList = pfcPrimaryList
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var paymentChannels = (request.PaymentChannels ?? [])
            .Where(channel => !string.IsNullOrWhiteSpace(channel))
            .Select(channel => channel.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();

        DateOnly? dateFrom = null;
        DateOnly? dateTo = null;

        if (!string.IsNullOrWhiteSpace(request.DateFrom))
        {
            if (!DateOnly.TryParse(
                    request.DateFrom.Trim(),
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var parsedDateFrom))
            {
                throw new ArgumentException("dateFrom must be an ISO date (YYYY-MM-DD).");
            }

            dateFrom = parsedDateFrom;
        }

        if (!string.IsNullOrWhiteSpace(request.DateTo))
        {
            if (!DateOnly.TryParse(
                    request.DateTo.Trim(),
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var parsedDateTo))
            {
                throw new ArgumentException("dateTo must be an ISO date (YYYY-MM-DD).");
            }

            dateTo = parsedDateTo;
        }

        var todayUtc = DateOnly.FromDateTime(DateTime.UtcNow);

        if (dateFrom is { } from && from > todayUtc)
            throw new ArgumentException("dateFrom cannot be in the future.");

        if (dateTo is { } to && to > todayUtc)
            throw new ArgumentException("dateTo cannot be in the future.");

        if (dateFrom is { } validatedFrom && dateTo is { } validatedTo && validatedFrom > validatedTo)
            throw new ArgumentException("dateFrom must be on or before dateTo.");

        var absAmountMin = request.AmountMin;
        var absAmountMax = request.AmountMax;

        if (absAmountMin is < 0)
            throw new ArgumentException("amountMin must be non-negative.");

        if (absAmountMax is < 0)
            throw new ArgumentException("amountMax must be non-negative.");

        if (absAmountMin is { } min && absAmountMax is { } max && min > max)
            throw new ArgumentException("amountMin must be less than or equal to amountMax.");

        TransactionFlow? amountFlow = null;
        if (!string.IsNullOrWhiteSpace(request.AmountFlow))
        {
            var raw = request.AmountFlow.Trim();
            if (raw.Equals("income", StringComparison.OrdinalIgnoreCase))
                amountFlow = TransactionFlow.Income;
            else if (raw.Equals("expense", StringComparison.OrdinalIgnoreCase))
                amountFlow = TransactionFlow.Expense;
            else
                throw new ArgumentException("amountFlow must be 'income' or 'expense'.");
        }

        return new TransactionQueryFilters
        {
            Offset = (page - 1) * pageSize,
            Limit = pageSize,
            SortBy = sortBy,
            Descending = descending,
            AccountIds = accountIds,
            PfcPrimaryList = distinctPfcPrimaryList,
            IncludePfcUncategorized = includePfcUncategorized,
            PaymentChannels = paymentChannels,
            Pending = request.Pending,
            DateFromInclusive = dateFrom,
            DateToInclusive = dateTo,
            AbsAmountMin = absAmountMin,
            AbsAmountMax = absAmountMax,
            AmountFlow = amountFlow
        };
    }

    private static readonly Dictionary<string, TransactionSortField> SortByFromQuery =
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

    private static TransactionSortField ParseSortByFromQuery(string raw)
    {
        if (!SortByFromQuery.TryGetValue(raw.Trim(), out var field))
            throw new ArgumentException(
                $"Invalid sortBy '{raw}'. Allowed values: {string.Join(", ", SortByFromQuery.Keys.Order(StringComparer.Ordinal))}.");

        return field;
    }

    private static bool ParseSortDirection(string raw)
    {
        if (raw.Equals("asc", StringComparison.OrdinalIgnoreCase))
            return false;

        if (raw.Equals("desc", StringComparison.OrdinalIgnoreCase))
            return true;

        throw new ArgumentException("sortDirection must be 'asc' or 'desc'.");
    }

    private static TransactionResponse ToResponse(Transaction transaction) => new()
    {
        Id = transaction.Id,
        LinkedBankAccountId = transaction.LinkedBankAccountId,
        PlaidTransactionId = transaction.PlaidTransactionId,
        Amount = transaction.Amount,
        IsoCurrencyCode = transaction.IsoCurrencyCode,
        Date = transaction.Date,
        AuthorizedDate = transaction.AuthorizedDate,
        AuthorizedDatetime = transaction.AuthorizedDatetime,
        Name = transaction.Name,
        MerchantName = transaction.MerchantName,
        Pending = transaction.Pending,
        PaymentChannel = transaction.PaymentChannel,
        PfcPrimary = transaction.PfcPrimary,
        PfcDetailed = transaction.PfcDetailed,
        LogoUrl = transaction.LogoUrl,
        Status = transaction.Status,
        RemovedAt = transaction.RemovedAt,
        CreatedAt = transaction.CreatedAt,
        UpdatedAt = transaction.UpdatedAt
    };
}