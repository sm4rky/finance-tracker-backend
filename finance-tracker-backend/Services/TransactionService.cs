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

public sealed class TransactionService(
    ITransactionRepository transactionRepository,
    ILinkedBankAccountRepository linkedBankAccountRepository,
    ILinkedBankRepository linkedBankRepository) : ITransactionService
{
    private const string IsoCurrencyCodeUsd = "USD";
    private const string PfcVersionDefault = "V2";
    private const int MaxDeleteTransactionBatchSize = 100;

    private sealed record TransactionDraft(
        Guid? LinkedBankAccountId,
        decimal Amount,
        DateOnly Date,
        string Name,
        string? MerchantName,
        bool Pending,
        string? PaymentChannel,
        string? PfcPrimary,
        string? PfcDetailed,
        string? Website,
        string Status);

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

        var descending = string.IsNullOrWhiteSpace(request.SortDirection)
                         || ParseSortDirectionOrThrow(request.SortDirection.Trim());

        if (TryEmptyPageIfFromAfterTo(request, page, pageSize, out var emptyPage))
            return emptyPage!;

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

    public async Task<TransactionResponse> CreateAsync(
        ClaimsPrincipal user,
        SaveTransactionRequest request,
        CancellationToken cancellationToken = default)
    {
        var profileId = user.RequireProfileId();
        var draft = await BuildDraftAsync(profileId, request, cancellationToken).ConfigureAwait(false);

        var now = DateTimeOffset.UtcNow;
        var row = new Transaction
        {
            Id = Guid.NewGuid(),
            ProfileId = profileId,
            PlaidTransactionId = null,
            IsoCurrencyCode = IsoCurrencyCodeUsd,
            AuthorizedDate = null,
            AuthorizedDatetime = null,
            MerchantEntityId = null,
            PendingTransactionId = null,
            TransactionType = null,
            PfcConfidenceLevel = null,
            PfcVersion = PfcVersionDefault,
            LogoUrl = null,
            RemovedAt = null,
            CreatedAt = now,
            UpdatedAt = now
        };

        ApplyDraft(row, draft);

        await transactionRepository.InsertAsync(row, cancellationToken).ConfigureAwait(false);
        return ToResponse(row);
    }

    public async Task<TransactionResponse> UpdateAsync(
        ClaimsPrincipal user,
        Guid transactionId,
        SaveTransactionRequest request,
        CancellationToken cancellationToken = default)
    {
        var profileId = user.RequireProfileId();

        var existing = await transactionRepository
            .GetByIdForProfileAsync(profileId, transactionId, cancellationToken)
            .ConfigureAwait(false);

        if (existing is null)
            throw new KeyNotFoundException("Transaction was not found.");

        if (existing.RemovedAt is not null)
            throw new ArgumentException("Removed transactions cannot be edited.");

        var draft = await BuildDraftAsync(profileId, request, cancellationToken).ConfigureAwait(false);

        ApplyDraft(existing, draft);

        if (request.ClearLogo)
            existing.LogoUrl = null;

        existing.UpdatedAt = DateTimeOffset.UtcNow;

        await transactionRepository.UpdateAsync(existing, cancellationToken).ConfigureAwait(false);
        return ToResponse(existing);
    }

    public async Task<DeleteTransactionsResponse> DeleteManyAsync(
        ClaimsPrincipal user,
        DeleteTransactionsRequest request,
        CancellationToken cancellationToken = default)
    {
        var profileId = user.RequireProfileId();

        var distinctIds = (request.TransactionIds)
            .Distinct()
            .ToList();

        switch (distinctIds.Count)
        {
            case 0:
                throw new ArgumentException("transactionIds must contain at least one id.");
            case > MaxDeleteTransactionBatchSize:
                throw new ArgumentException(
                    $"transactionIds cannot contain more than {MaxDeleteTransactionBatchSize} ids.");
            default:
            {
                var deletedCount = await transactionRepository
                    .DeleteByIdsForProfileAsync(profileId, distinctIds, cancellationToken)
                    .ConfigureAwait(false);

                return new DeleteTransactionsResponse { DeletedCount = deletedCount };
            }
        }
    }

    private async Task<TransactionDraft> BuildDraftAsync(
        Guid profileId,
        SaveTransactionRequest request,
        CancellationToken cancellationToken)
    {
        var linkedBankAccountId = await EnsureLinkedBankAccountBelongsToProfileAsync(
                profileId,
                request.LinkedBankAccountId,
                cancellationToken)
            .ConfigureAwait(false);

        if (request.Amount < 0)
            throw new ArgumentException("amount must be greater than or equal to 0.");

        var amountFlow = TransactionQueryFilterHelper.ParseAndValidateAmountFlow(request.AmountFlow);
        var merchantName = TrimToNull(request.MerchantName);

        return new TransactionDraft(
            LinkedBankAccountId: linkedBankAccountId,
            Amount: ApplyAmountFlowSign(request.Amount, amountFlow),
            Date: ParseAndValidateDate(request.Date),
            Name: merchantName ?? "",
            MerchantName: merchantName,
            Pending: request.Pending,
            PaymentChannel: TrimToNull(request.PaymentChannel),
            PfcPrimary: NormalizePfcPrimaryOrNull(request.PfcPrimary),
            PfcDetailed: TrimToNull(request.PfcDetailed),
            Website: TrimToNull(request.Website),
            Status: ParseAndValidateStatus(request.Status));
    }

    private static void ApplyDraft(Transaction transaction, TransactionDraft draft)
    {
        transaction.LinkedBankAccountId = draft.LinkedBankAccountId;
        transaction.Amount = draft.Amount;
        transaction.IsoCurrencyCode = IsoCurrencyCodeUsd;
        transaction.Date = draft.Date;
        transaction.Name = draft.Name;
        transaction.MerchantName = draft.MerchantName;
        transaction.Pending = draft.Pending;
        transaction.PaymentChannel = draft.PaymentChannel;
        transaction.PfcPrimary = draft.PfcPrimary;
        transaction.PfcDetailed = draft.PfcDetailed;
        transaction.Website = draft.Website;
        transaction.Status = draft.Status;
        transaction.PfcVersion = PfcVersionDefault;
    }

    private async Task<Guid?> EnsureLinkedBankAccountBelongsToProfileAsync(
        Guid profileId,
        Guid? linkedBankAccountId,
        CancellationToken cancellationToken)
    {
        if (linkedBankAccountId is null)
            return null;

        var account = await linkedBankAccountRepository
            .GetByIdAsync(linkedBankAccountId.Value, cancellationToken)
            .ConfigureAwait(false);

        if (account is null)
            throw new KeyNotFoundException("Linked bank account was not found.");

        var bank = await linkedBankRepository
            .GetByIdForProfileAsync(account.LinkedBankId, profileId, cancellationToken)
            .ConfigureAwait(false);

        if (bank is null)
            throw new ArgumentException("Linked bank account does not belong to your profile.");

        return account.Id;
    }

    private static string? TrimToNull(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string ParseAndValidateStatus(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            throw new ArgumentException("status is required.");

        var value = raw.Trim();

        if (value.Equals("active", StringComparison.OrdinalIgnoreCase))
            return "active";

        if (value.Equals("account_opted_out", StringComparison.OrdinalIgnoreCase))
            return "account_opted_out";

        throw new ArgumentException("status must be 'active' or 'account_opted_out'.");
    }

    private static decimal ApplyAmountFlowSign(decimal amount, TransactionFlow flow)
    {
        var absoluteAmount = Math.Abs(amount);
        return flow == TransactionFlow.Income ? -absoluteAmount : absoluteAmount;
    }

    private static DateOnly ParseAndValidateDate(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            throw new ArgumentException("date is required.");

        if (!DateOnly.TryParse(
                raw.Trim(),
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var date))
        {
            throw new ArgumentException("date must be an ISO date (YYYY-MM-DD).");
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (date > today)
            throw new ArgumentException("date cannot be in the future.");

        return date;
    }

    private static string? NormalizePfcPrimaryOrNull(string? raw)
    {
        var value = TrimToNull(raw);
        if (value is null)
            return null;

        return string.Equals(value, TransactionQueryFilterHelper.PfcPrimaryUncategorizedQueryValue,
            StringComparison.OrdinalIgnoreCase)
            ? null
            : value;
    }

    private static bool TryEmptyPageIfFromAfterTo(
        QueryTransactionsRequest request,
        int page,
        int pageSize,
        out PagedResponse<TransactionResponse>? emptyPage)
    {
        emptyPage = null;

        if (string.IsNullOrWhiteSpace(request.DateFrom) || string.IsNullOrWhiteSpace(request.DateTo))
            return false;

        if (!DateOnly.TryParse(
                request.DateFrom.Trim(),
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var df))
            return false;

        if (!DateOnly.TryParse(
                request.DateTo.Trim(),
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var dt))
            return false;

        var todayUtc = DateOnly.FromDateTime(DateTime.UtcNow);
        if (df > todayUtc)
            throw new ArgumentException("dateFrom cannot be in the future.");
        if (dt > todayUtc)
            throw new ArgumentException("dateTo cannot be in the future.");

        if (df <= dt)
            return false;

        emptyPage = new PagedResponse<TransactionResponse>
        {
            Items = [],
            TotalCount = 0,
            Page = page,
            PageSize = pageSize
        };
        return true;
    }

    private static TransactionQueryFilters BuildTransactionQuery(
        QueryTransactionsRequest request,
        int page,
        int pageSize,
        TransactionSortField sortBy,
        bool descending) =>
        TransactionQueryFilterHelper.CreateForPagedQuery(request, page, pageSize, sortBy, descending);

    private static TransactionSortField ParseSortByFromQuery(string raw)
    {
        if (!SortByFromQuery.TryGetValue(raw.Trim(), out var field))
        {
            throw new ArgumentException(
                $"Invalid sortBy '{raw}'. Allowed values: {string.Join(", ", SortByFromQuery.Keys.Order(StringComparer.Ordinal))}.");
        }

        return field;
    }

    private static bool ParseSortDirectionOrThrow(string raw)
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
        Website = transaction.Website,
        LogoUrl = transaction.LogoUrl,
        Status = transaction.Status,
        RemovedAt = transaction.RemovedAt,
        CreatedAt = transaction.CreatedAt,
        UpdatedAt = transaction.UpdatedAt
    };
}