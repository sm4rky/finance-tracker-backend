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
    ILinkedBankRepository linkedBankRepository,
    CustomCategorySetHelper customCategorySetHelper,
    IBudgetPeriodRefreshService budgetPeriodRefreshService) : ITransactionService
{
    private const string IsoCurrencyCodeUsd = "USD";
    private const int MaxDeleteTransactionBatchSize = 100;

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

        var customCategorySetData = request.CustomCategorySetId is { } customCategorySetId
            ? await customCategorySetHelper
                .LoadCustomCategoryByPfcPrimaryAsync(profileId, customCategorySetId, request.CustomCategoryIds,
                    cancellationToken)
                .ConfigureAwait(false)
            : null;

        var customPfcPrimaryList = customCategorySetData is not null && request.CustomCategoryIds is { Count: > 0 }
            ? customCategorySetData.Keys
                .Select(pfcPrimary => pfcPrimary.PfcPrimaryCode)
                .Where(code => !string.IsNullOrWhiteSpace(code))
                .Distinct(StringComparer.Ordinal)
                .ToList()
            : null;

        if (customPfcPrimaryList is not null)
            request.PfcPrimaryList = customPfcPrimaryList;

        var query = TransactionsQueryHelper.CreateForPagedTransactionsQuery(request);

        var totalCount = await transactionRepository
            .CountAsync(profileId, query, cancellationToken)
            .ConfigureAwait(false);

        var rows = await transactionRepository
            .QueryPagedAsync(profileId, query, cancellationToken)
            .ConfigureAwait(false);

        var accountIds = rows
            .Select(row => row.LinkedBankAccountId)
            .Where(linkedBankAccountId => linkedBankAccountId is not null)
            .Cast<Guid>()
            .Distinct()
            .ToList();
        var accountMap = await LoadAccountMapAsync(profileId, accountIds, cancellationToken).ConfigureAwait(false);

        return new PagedResponse<TransactionResponse>
        {
            Items = rows
                .Select(row => ToTransactionResponse(
                    row,
                    row.LinkedBankAccountId is { } linkedBankAccountId
                        ? accountMap.GetValueOrDefault(linkedBankAccountId)
                        : null,
                    customCategorySetData))
                .ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<IReadOnlyList<TransactionResponse>> GetRecentAsync(
        ClaimsPrincipal user,
        int? limit = null,
        CancellationToken cancellationToken = default)
    {
        var profileId = user.RequireProfileId();
        var take = limit switch
        {
            null => 5,
            < 1 => throw new ArgumentException("limit must be at least 1."),
            _ => limit.Value
        };

        var rows = await transactionRepository
            .ListRecentForProfileAsync(profileId, take, cancellationToken)
            .ConfigureAwait(false);

        var accountIds = rows
            .Select(row => row.LinkedBankAccountId)
            .Where(linkedBankAccountId => linkedBankAccountId is not null)
            .Cast<Guid>()
            .Distinct()
            .ToList();
        var accountMap = await LoadAccountMapAsync(profileId, accountIds, cancellationToken).ConfigureAwait(false);

        return rows
            .Select(row => ToTransactionResponse(
                row,
                row.LinkedBankAccountId is { } linkedBankAccountId
                    ? accountMap.GetValueOrDefault(linkedBankAccountId)
                    : null,
                customCategoryByPfcPrimary: null))
            .ToList();
    }

    public async Task<TransactionResponse> CreateAsync(
        ClaimsPrincipal user,
        SaveTransactionRequest request,
        CancellationToken cancellationToken = default)
    {
        var profileId = user.RequireProfileId();
        var linkedBankAccountId = await EnsureLinkedBankAccountBelongsToProfileAsync(
                profileId,
                request.LinkedBankAccountId,
                cancellationToken)
            .ConfigureAwait(false);

        if (request.Amount < 0)
            throw new ArgumentException("amount must be greater than or equal to 0.");

        var amountFlow = TransactionsQueryHelper.ParseTransactionFlow(request.AmountFlow);
        var amount = Math.Abs(request.Amount) * (amountFlow == TransactionFlow.Income ? -1 : 1);
        var merchantName = string.IsNullOrWhiteSpace(request.MerchantName) ? null : request.MerchantName.Trim();
        var now = DateTimeOffset.UtcNow;
        var row = new Transaction
        {
            Id = Guid.NewGuid(),
            ProfileId = profileId,
            LinkedBankAccountId = linkedBankAccountId,
            PlaidTransactionId = null,
            Amount = amount,
            IsoCurrencyCode = IsoCurrencyCodeUsd,
            Date = ParseAndValidateDate(request.Date),
            AuthorizedDate = null,
            AuthorizedDatetime = null,
            MerchantEntityId = null,
            PendingTransactionId = null,
            TransactionType = null,
            PfcConfidenceLevel = null,
            PfcVersion = CustomCategorySetHelper.PfcVersion,
            Name = merchantName ?? "",
            MerchantName = merchantName,
            Pending = request.Pending,
            PaymentChannel = string.IsNullOrWhiteSpace(request.PaymentChannel) ? null : request.PaymentChannel.Trim(),
            PfcPrimary = NormalizePfcPrimaryOrNull(request.PfcPrimary),
            PfcDetailed = string.IsNullOrWhiteSpace(request.PfcDetailed) ? null : request.PfcDetailed.Trim(),
            Website = string.IsNullOrWhiteSpace(request.Website) ? null : request.Website.Trim(),
            LogoUrl = null,
            Status = ParseAndValidateStatus(request.Status),
            RemovedAt = null,
            CreatedAt = now,
            UpdatedAt = now
        };

        await transactionRepository.InsertAsync(row, cancellationToken).ConfigureAwait(false);
        await budgetPeriodRefreshService
            .RefreshForProfileDatesAsync(profileId, [row.Date], cancellationToken)
            .ConfigureAwait(false);

        return ToTransactionResponse(row, account: null, customCategoryByPfcPrimary: null);
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

        var previousDate = existing.Date;

        var linkedBankAccountId = await EnsureLinkedBankAccountBelongsToProfileAsync(
                profileId,
                request.LinkedBankAccountId,
                cancellationToken)
            .ConfigureAwait(false);

        if (request.Amount < 0)
            throw new ArgumentException("amount must be greater than or equal to 0.");

        var amountFlow = TransactionsQueryHelper.ParseTransactionFlow(request.AmountFlow);
        var amount = Math.Abs(request.Amount) * (amountFlow == TransactionFlow.Income ? -1 : 1);
        var merchantName = string.IsNullOrWhiteSpace(request.MerchantName) ? null : request.MerchantName.Trim();

        existing.LinkedBankAccountId = linkedBankAccountId;
        existing.Amount = amount;
        existing.IsoCurrencyCode = IsoCurrencyCodeUsd;
        existing.Date = ParseAndValidateDate(request.Date);
        existing.Name = merchantName ?? "";
        existing.MerchantName = merchantName;
        existing.Pending = request.Pending;
        existing.PaymentChannel =
            string.IsNullOrWhiteSpace(request.PaymentChannel) ? null : request.PaymentChannel.Trim();
        existing.PfcPrimary = NormalizePfcPrimaryOrNull(request.PfcPrimary);
        existing.PfcDetailed = string.IsNullOrWhiteSpace(request.PfcDetailed) ? null : request.PfcDetailed.Trim();
        existing.Website = string.IsNullOrWhiteSpace(request.Website) ? null : request.Website.Trim();
        existing.Status = ParseAndValidateStatus(request.Status);
        existing.PfcVersion = CustomCategorySetHelper.PfcVersion;

        if (request.ClearLogo)
            existing.LogoUrl = null;

        existing.UpdatedAt = DateTimeOffset.UtcNow;

        await transactionRepository.UpdateAsync(existing, cancellationToken).ConfigureAwait(false);
        await budgetPeriodRefreshService
            .RefreshForProfileDatesAsync(profileId, [previousDate, existing.Date], cancellationToken)
            .ConfigureAwait(false);

        return ToTransactionResponse(existing, account: null, customCategoryByPfcPrimary: null);
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
                var affectedDates = new List<DateOnly>();
                foreach (var transactionId in distinctIds)
                {
                    var transaction = await transactionRepository
                        .GetByIdForProfileAsync(profileId, transactionId, cancellationToken)
                        .ConfigureAwait(false);
                    if (transaction is not null)
                        affectedDates.Add(transaction.Date);
                }

                var deletedCount = await transactionRepository
                    .DeleteByIdsForProfileAsync(profileId, distinctIds, cancellationToken)
                    .ConfigureAwait(false);

                if (affectedDates.Count > 0)
                {
                    await budgetPeriodRefreshService
                        .RefreshForProfileDatesAsync(profileId, affectedDates, cancellationToken)
                        .ConfigureAwait(false);
                }

                return new DeleteTransactionsResponse { DeletedCount = deletedCount };
            }
        }
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

        return bank is null
            ? throw new ArgumentException("Linked bank account does not belong to your profile.")
            : account.Id;
    }

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
        return date > today ? throw new ArgumentException("date cannot be in the future.") : date;
    }

    private static string? NormalizePfcPrimaryOrNull(string? raw)
    {
        var value = string.IsNullOrWhiteSpace(raw) ? null : raw.Trim();
        if (value is null)
            return null;

        return string.Equals(value, TransactionsQueryHelper.PfcPrimaryUncategorizedQueryValue,
            StringComparison.OrdinalIgnoreCase)
            ? null
            : value;
    }

    private async Task<Dictionary<Guid, LinkedBankAccount>> LoadAccountMapAsync(
        Guid profileId,
        IReadOnlyCollection<Guid> accountIds,
        CancellationToken cancellationToken)
    {
        var map = new Dictionary<Guid, LinkedBankAccount>();
        foreach (var id in accountIds.Distinct())
        {
            var linkedBankAccount =
                await linkedBankAccountRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
            if (linkedBankAccount is null)
                continue;

            var bank = await linkedBankRepository
                .GetByIdForProfileAsync(linkedBankAccount.LinkedBankId, profileId, cancellationToken)
                .ConfigureAwait(false);

            if (bank is not null)
                map[id] = linkedBankAccount;
        }

        return map;
    }

    private static TransactionResponse ToTransactionResponse(
        Transaction transaction,
        LinkedBankAccount? account,
        IReadOnlyDictionary<ProfileCustomCategoryPfcPrimary, ProfileCustomCategoryResponse>?
            customCategoryByPfcPrimary) => new()
    {
        Id = transaction.Id,
        LinkedBankAccount = ToLinkedBankAccountResponse(account),
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
        CustomCategory = customCategoryByPfcPrimary is null || string.IsNullOrWhiteSpace(transaction.PfcPrimary)
            ? null
            : CustomCategorySetHelper.GetCustomCategoryResponseByPfcPrimaryCodeAndPfcVersion(
                customCategoryByPfcPrimary,
                transaction.PfcPrimary,
                transaction.PfcVersion),
        Website = transaction.Website,
        LogoUrl = transaction.LogoUrl,
        Status = transaction.Status,
        RemovedAt = transaction.RemovedAt,
        CreatedAt = transaction.CreatedAt,
        UpdatedAt = transaction.UpdatedAt
    };

    private static TransactionLinkedBankAccountResponse? ToLinkedBankAccountResponse(LinkedBankAccount? account)
    {
        if (account is null)
            return null;

        return new TransactionLinkedBankAccountResponse
        {
            Id = account.Id,
            AccountName = account.AccountName,
            OfficialName = account.OfficialName,
            Mask = account.Mask,
            Type = account.Type,
            Subtype = account.Subtype
        };
    }
}