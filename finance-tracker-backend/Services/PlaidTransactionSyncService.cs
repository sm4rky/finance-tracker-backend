using System.Security.Claims;
using System.Text.RegularExpressions;
using finance_tracker_backend.Contracts.Responses;
using finance_tracker_backend.Infrastructure;
using finance_tracker_backend.Middleware;
using finance_tracker_backend.Models;
using finance_tracker_backend.Repositories;
using Going.Plaid;
using Microsoft.Extensions.Configuration;
using Going.Plaid.Entity;
using Going.Plaid.Transactions;
using PlaidTransaction = Going.Plaid.Entity.Transaction;

namespace finance_tracker_backend.Services;

public sealed class PlaidTransactionSyncService(
    PlaidClient plaidClient,
    IConfiguration configuration,
    ILinkedBankRepository linkedBankRepository,
    ILinkedBankAccountRepository linkedBankAccountRepository,
    ITransactionRepository transactionRepository,
    PlaidAccessTokenProtector tokenProtector,
    ILogger<PlaidTransactionSyncService> logger) : IPlaidTransactionSyncService
{
    private const int DefaultPlaidSyncBatchSize = 200;

    private static readonly TimeSpan MinIntervalBetweenSyncs = TimeSpan.FromMinutes(30);

    public Task<SyncPlaidTransactionsResponse> SyncLinkedBankAsync(
        ClaimsPrincipal user,
        Guid linkedBankId,
        CancellationToken cancellationToken = default,
        bool bypassCooldown = false) =>
        SyncLinkedBankForProfileAsync(user.RequireProfileId(), linkedBankId, cancellationToken, bypassCooldown);

    public async Task<SyncPlaidTransactionsResponse> SyncLinkedBankForProfileAsync(
        Guid profileId,
        Guid linkedBankId,
        CancellationToken cancellationToken = default,
        bool bypassCooldown = true)
    {
        var bank = await linkedBankRepository.GetByIdForProfileAsync(linkedBankId, profileId, cancellationToken)
            .ConfigureAwait(false);
        if (bank is null)
            throw new KeyNotFoundException("Linked bank was not found.");

        if (string.IsNullOrWhiteSpace(bank.PlaidAccessTokenEncrypted))
            throw new InvalidOperationException("This connection has no Plaid access token.");

        var now = DateTimeOffset.UtcNow;
        if (!bypassCooldown && bank.LastSyncedAt is { } lastSynced)
        {
            var elapsed = now - lastSynced;
            if (elapsed < MinIntervalBetweenSyncs)
            {
                var retryAfter = MinIntervalBetweenSyncs - elapsed;
                throw new SyncCooldownException(
                    $"Please wait {(int)Math.Ceiling(retryAfter.TotalMinutes)} minute(s) before syncing again.",
                    retryAfter);
            }
        }

        var accessToken = tokenProtector.Unprotect(bank.PlaidAccessTokenEncrypted);

        string? lastStatus = null;
        var cursor = bank.PlaidTransactionsCursor;

        while (true)
        {
            var request = new TransactionsSyncRequest
            {
                AccessToken = accessToken,
                Count = 500,
                Options = new TransactionsSyncRequestOptions
                {
                    PersonalFinanceCategoryVersion = PersonalFinanceCategoryVersion.V2
                }
            };
            if (!string.IsNullOrEmpty(cursor))
                request.Cursor = cursor;

            var response = await plaidClient.TransactionsSyncAsync(request).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                var errorCode = response.Error?.ErrorCode?.ToString();
                if (IsItemLoginRequiredError(errorCode, response.Error?.ErrorMessage))
                {
                    bank.Status = "relink_required";
                    bank.UpdatedAt = DateTimeOffset.UtcNow;
                    await linkedBankRepository.UpdateAsync(bank, cancellationToken).ConfigureAwait(false);
                    logger.LogWarning(
                        "Plaid ITEM_LOGIN_REQUIRED; linked_bank {LinkedBankId} set to relink_required (request_id={RequestId})",
                        bank.Id,
                        response.RequestId);
                    throw new PlaidItemRelinkRequiredException(
                        "This bank connection must be updated in Plaid Link (sign in again). The connection is marked as requiring relink.");
                }

                var detail = response.Error?.ErrorMessage ?? errorCode ?? "Unknown Plaid error";
                logger.LogWarning("Plaid transactions/sync failed: {Detail} (request_id={RequestId})", detail,
                    response.RequestId);
                throw new InvalidOperationException($"Plaid transactions/sync failed: {detail}");
            }

            lastStatus = response.TransactionsUpdateStatus.ToString();

            foreach (var t in response.Added ?? [])
                await UpsertFromPlaidAsync(t, profileId, linkedBankId, cancellationToken).ConfigureAwait(false);

            foreach (var t in response.Modified ?? [])
                await UpsertFromPlaidAsync(t, profileId, linkedBankId, cancellationToken).ConfigureAwait(false);

            foreach (var r in response.Removed ?? [])
            {
                if (string.IsNullOrWhiteSpace(r.TransactionId))
                    continue;
                await transactionRepository.SetRemovedAtAsync(
                        profileId,
                        r.TransactionId.Trim(),
                        DateTimeOffset.UtcNow,
                        cancellationToken)
                    .ConfigureAwait(false);
            }

            cursor = response.NextCursor;
            if (response.HasMore) continue;
            if (!string.IsNullOrEmpty(response.NextCursor))
                bank.PlaidTransactionsCursor = response.NextCursor;
            break;
        }

        var syncedAt = DateTimeOffset.UtcNow;
        bank.LastSyncedAt = syncedAt;
        bank.UpdatedAt = syncedAt;
        if (bank.Status == "relink_required")
            bank.Status = "active";
        await linkedBankRepository.UpdateAsync(bank, cancellationToken).ConfigureAwait(false);

        return new SyncPlaidTransactionsResponse
        {
            LinkedBankId = linkedBankId,
            TransactionsUpdateStatus = lastStatus,
            SyncedAt = syncedAt
        };
    }

    public async Task SyncActiveLinkedBanksAsync(CancellationToken cancellationToken = default)
    {
        var batchSize = configuration.GetValue("Hangfire:PlaidSyncBatchSize", DefaultPlaidSyncBatchSize);
        if (batchSize < 1)
            batchSize = DefaultPlaidSyncBatchSize;

        var afterBankId = Guid.Empty;
        var ok = 0;
        var failed = 0;
        var batches = 0;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var batch = await linkedBankRepository
                .ListActiveLinkedBanksAfterIdAsync(afterBankId, batchSize, cancellationToken)
                .ConfigureAwait(false);

            if (batch.Count == 0)
                break;

            batches++;

            foreach (var (profileId, linkedBankId) in batch)
            {
                try
                {
                    await SyncLinkedBankForProfileAsync(profileId, linkedBankId, cancellationToken,
                            bypassCooldown: true)
                        .ConfigureAwait(false);
                    ok++;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    failed++;
                    logger.LogWarning(
                        ex,
                        "Plaid transaction sync failed for linked_bank {LinkedBankId} profile {ProfileId}; continuing with next bank.",
                        linkedBankId,
                        profileId);
                }
            }

            afterBankId = batch[^1].LinkedBankId;
            if (batch.Count < batchSize)
                break;
        }

        logger.LogInformation(
            "Plaid transaction sync job finished: {Ok} ok, {Failed} failed, {Batches} batch(es), batch size {BatchSize}.",
            ok,
            failed,
            batches,
            batchSize);
    }

    private async Task<(Guid? AccountId, string Status)> ResolveAccountAsync(
        Guid linkedBankId,
        string? plaidAccountId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(plaidAccountId))
            return (null, "active");

        var acc = await linkedBankAccountRepository
            .GetByLinkedBankAndPlaidAccountIdAsync(linkedBankId, plaidAccountId.Trim(), cancellationToken)
            .ConfigureAwait(false);
        if (acc is null)
            return (null, "active");

        return (acc.Id, acc.IsActive ? "active" : "account_opted_out");
    }

    private async Task<bool> UpsertFromPlaidAsync(
        PlaidTransaction p,
        Guid profileId,
        Guid linkedBankId,
        CancellationToken cancellationToken)
    {
        var tid = p.TransactionId?.Trim();
        if (string.IsNullOrEmpty(tid))
            return false;

        if (!p.Date.HasValue)
        {
            logger.LogWarning("Skipping Plaid transaction without date (transaction_id={TransactionId})", tid);
            return false;
        }

        var (linkedAccountId, status) = await ResolveAccountAsync(linkedBankId, p.AccountId, cancellationToken)
            .ConfigureAwait(false);
        var now = DateTimeOffset.UtcNow;
        var existing = await transactionRepository
            .GetByProfileAndPlaidTransactionIdAsync(profileId, tid, cancellationToken)
            .ConfigureAwait(false);

        if (existing is null)
        {
            var normMerchant = ComputeNormalizedMerchantFingerprint(p);
            var duplicate = await transactionRepository.FindActiveDuplicateForFingerprintAsync(
                    profileId,
                    p.Date!.Value,
                    p.Amount ?? 0,
                    normMerchant,
                    tid,
                    cancellationToken)
                .ConfigureAwait(false);

            if (duplicate is not null)
            {
                MapPlaidToRow(duplicate, p, linkedAccountId, status, now);
                await transactionRepository.UpdateAsync(duplicate, cancellationToken).ConfigureAwait(false);
                return true;
            }

            var row = new Models.Transaction
            {
                Id = Guid.NewGuid(),
                ProfileId = profileId,
                CreatedAt = now
            };
            MapPlaidToRow(row, p, linkedAccountId, status, now);
            await transactionRepository.InsertAsync(row, cancellationToken).ConfigureAwait(false);
            return true;
        }

        MapPlaidToRow(existing, p, linkedAccountId, status, now);
        await transactionRepository.UpdateAsync(existing, cancellationToken).ConfigureAwait(false);
        return true;
    }

    private static string ComputeNormalizedMerchantFingerprint(PlaidTransaction p)
    {
        var nameField = p.MerchantName ?? p.Name ?? p.OriginalDescription ?? string.Empty;
        var raw = string.IsNullOrWhiteSpace(p.MerchantName) ? nameField.Trim() : p.MerchantName.Trim();
        raw = Regex.Replace(raw, @"\s+", " ").Trim();
        return raw.ToLowerInvariant();
    }

    private static void MapPlaidToRow(
        Models.Transaction row,
        PlaidTransaction p,
        Guid? linkedBankAccountId,
        string status,
        DateTimeOffset now)
    {
        row.LinkedBankAccountId = linkedBankAccountId;
        row.PlaidTransactionId = p.TransactionId!.Trim();
        row.Amount = p.Amount ?? 0;
        row.IsoCurrencyCode = string.IsNullOrWhiteSpace(p.IsoCurrencyCode) ? null : p.IsoCurrencyCode.Trim();
        row.Date = p.Date!.Value;
        row.AuthorizedDate = p.AuthorizedDate;
        row.AuthorizedDatetime = p.AuthorizedDatetime;
        row.Name = string.IsNullOrWhiteSpace(p.Name) ? null : p.Name.Trim();
        row.MerchantName = string.IsNullOrWhiteSpace(p.MerchantName) ? null : p.MerchantName.Trim();
        row.MerchantEntityId = string.IsNullOrWhiteSpace(p.MerchantEntityId) ? null : p.MerchantEntityId.Trim();
        row.Pending = p.Pending ?? false;
        row.PendingTransactionId = string.IsNullOrWhiteSpace(p.PendingTransactionId)
            ? null
            : p.PendingTransactionId.Trim();
        row.PaymentChannel = p.PaymentChannel?.ToString();
        row.TransactionType = p.TransactionCode?.ToString();
        var pfc = p.PersonalFinanceCategory;
        row.PfcPrimary = pfc?.Primary;
        row.PfcDetailed = pfc?.Detailed;
        row.PfcConfidenceLevel = pfc?.ConfidenceLevel;
        row.PfcVersion = pfc?.Version?.ToString();
        row.LogoUrl = string.IsNullOrWhiteSpace(p.LogoUrl) ? null : p.LogoUrl.Trim();
        row.Website = string.IsNullOrWhiteSpace(p.Website) ? null : p.Website.Trim();
        row.Status = status;
        row.RemovedAt = null;
        row.UpdatedAt = now;
    }

    private static bool IsItemLoginRequiredError(string? errorCode, string? errorMessage)
    {
        if (!string.IsNullOrEmpty(errorCode) &&
            string.Equals(errorCode, "ITEM_LOGIN_REQUIRED", StringComparison.OrdinalIgnoreCase))
            return true;

        if (string.IsNullOrEmpty(errorMessage))
            return false;

        return errorMessage.Contains("ITEM_LOGIN_REQUIRED", StringComparison.OrdinalIgnoreCase)
               || errorMessage.Contains("user login is required", StringComparison.OrdinalIgnoreCase)
               || errorMessage.Contains("update mode", StringComparison.OrdinalIgnoreCase);
    }
}