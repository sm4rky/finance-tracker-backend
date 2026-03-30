using System.Security.Claims;
using finance_tracker_backend.Contracts.Pagination;
using finance_tracker_backend.Contracts.Requests;
using finance_tracker_backend.Contracts.Responses;
using finance_tracker_backend.Enums;
using finance_tracker_backend.Infrastructure;
using finance_tracker_backend.Models;
using finance_tracker_backend.Repositories;

namespace finance_tracker_backend.Services;

public sealed class TransactionService(ITransactionRepository transactionRepository) : ITransactionService
{
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

        TransactionSortByField sortField;
        bool descending;

        if (string.IsNullOrWhiteSpace(request.SortBy))
        {
            sortField = TransactionSortByField.Date;
            descending = true;
        }
        else
        {
            sortField = ParseSortByFromQuery(request.SortBy);

            descending = string.IsNullOrWhiteSpace(request.SortDirection) || ParseSortDirection(request.SortDirection.Trim());
        }

        var totalCount = await transactionRepository
            .CountAsync(profileId, cancellationToken)
            .ConfigureAwait(false);

        var offset = (page - 1) * pageSize;
        var rows = await transactionRepository
            .QueryPagedAsync(profileId, offset, pageSize, sortField, descending, cancellationToken)
            .ConfigureAwait(false);

        return new PagedResponse<TransactionResponse>
        {
            Items = rows.Select(ToResponse).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    private static readonly Dictionary<string, TransactionSortByField> SortByFromQuery =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["merchantName"] = TransactionSortByField.MerchantName,
            ["linkedBankAccountId"] = TransactionSortByField.LinkedBankAccountId,
            ["pfcPrimary"] = TransactionSortByField.PfcPrimary,
            ["pfcDetailed"] = TransactionSortByField.PfcDetailed,
            ["date"] = TransactionSortByField.Date,
            ["amount"] = TransactionSortByField.Amount,
            ["paymentChannel"] = TransactionSortByField.PaymentChannel,
            ["pending"] = TransactionSortByField.Pending
        };

    private static TransactionSortByField ParseSortByFromQuery(string raw)
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

    private static TransactionResponse ToResponse(Transaction t) => new()
    {
        Id = t.Id,
        LinkedBankAccountId = t.LinkedBankAccountId,
        PlaidTransactionId = t.PlaidTransactionId,
        Amount = t.Amount,
        IsoCurrencyCode = t.IsoCurrencyCode,
        Date = t.Date,
        AuthorizedDate = t.AuthorizedDate,
        AuthorizedDatetime = t.AuthorizedDatetime,
        Name = t.Name,
        MerchantName = t.MerchantName,
        Pending = t.Pending,
        PaymentChannel = t.PaymentChannel,
        PfcPrimary = t.PfcPrimary,
        PfcDetailed = t.PfcDetailed,
        LogoUrl = t.LogoUrl,
        Status = t.Status,
        RemovedAt = t.RemovedAt,
        CreatedAt = t.CreatedAt,
        UpdatedAt = t.UpdatedAt
    };
}
