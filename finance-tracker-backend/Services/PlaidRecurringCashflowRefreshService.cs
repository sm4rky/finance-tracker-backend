using System.Security.Claims;
using finance_tracker_backend.Contracts.Responses;
using finance_tracker_backend.Infrastructure;
using finance_tracker_backend.Models;
using finance_tracker_backend.Repositories;
using Going.Plaid;
using Going.Plaid.Entity;
using Going.Plaid.Transactions;

namespace finance_tracker_backend.Services;

public sealed class PlaidRecurringCashflowRefreshService(
    PlaidClient plaidClient,
    ILinkedBankRepository linkedBankRepository,
    ILinkedBankAccountRepository linkedBankAccountRepository,
    IProfileRecurringCashflowRepository recurringRepository,
    PlaidAccessTokenProtector tokenProtector,
    ILogger<PlaidRecurringCashflowRefreshService> logger) : IPlaidRecurringCashflowRefreshService
{
    public async Task<SyncPlaidRecurringCashflowsResponse> SyncLinkedBankRecurringCashflowsAsync(
        ClaimsPrincipal user,
        Guid linkedBankId,
        CancellationToken cancellationToken = default)
    {
        var profileId = user.RequireProfileId();
        var bank = await linkedBankRepository
            .GetByIdForProfileAsync(linkedBankId, profileId, cancellationToken)
            .ConfigureAwait(false);

        if (bank is null)
            throw new KeyNotFoundException("Linked bank was not found.");

        if (string.IsNullOrWhiteSpace(bank.PlaidAccessTokenEncrypted))
            throw new InvalidOperationException("This connection has no Plaid access token.");

        var accessToken = tokenProtector.Unprotect(bank.PlaidAccessTokenEncrypted);

        var response = await plaidClient
            .TransactionsRecurringGetAsync(new TransactionsRecurringGetRequest { AccessToken = accessToken })
            .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var detail = response.Error?.ErrorMessage ?? response.Error?.ErrorCode ?? "Unknown Plaid error";
            logger.LogWarning("Plaid transactions/recurring/get failed: {Detail} (request_id={RequestId})", detail, response.RequestId);
            throw new InvalidOperationException($"Could not refresh recurring cashflows: {detail}");
        }

        var inflows = response.InflowStreams;
        var outflows = response.OutflowStreams;

        foreach (var stream in inflows)
            await UpsertStreamAsync(profileId, linkedBankId, stream, "inflow", cancellationToken).ConfigureAwait(false);

        foreach (var stream in outflows)
            await UpsertStreamAsync(profileId, linkedBankId, stream, "outflow", cancellationToken).ConfigureAwait(false);

        var syncedAt = DateTimeOffset.UtcNow;
        return new SyncPlaidRecurringCashflowsResponse
        {
            LinkedBankId = linkedBankId,
            SyncedAt = syncedAt
        };
    }

    private async Task UpsertStreamAsync(
        Guid profileId,
        Guid linkedBankId,
        TransactionStream stream,
        string direction,
        CancellationToken cancellationToken)
    {
        var streamId = stream.StreamId?.Trim();
        if (string.IsNullOrEmpty(streamId))
            return;

        var plaidAccountId = stream.AccountId?.Trim();
        LinkedBankAccount? account = null;
        if (!string.IsNullOrEmpty(plaidAccountId))
        {
            account = await linkedBankAccountRepository
                .GetByLinkedBankAndPlaidAccountIdAsync(linkedBankId, plaidAccountId, cancellationToken)
                .ConfigureAwait(false);
        }

        var (linkedId, accountActive) = account is null
            ? ((Guid?)null, false)
            : (account.Id, account.IsActive);

        var appStatus = MapAppStatus(stream, linkedId, accountActive);

        var merchantName = string.IsNullOrWhiteSpace(stream.MerchantName) ? null : stream.MerchantName.Trim();
        var description = string.IsNullOrWhiteSpace(stream.Description) ? null : stream.Description.Trim();

        var pfc = stream.PersonalFinanceCategory;
        var pfcPrimary = string.IsNullOrWhiteSpace(pfc?.Primary) ? null : pfc.Primary.Trim();
        var pfcDetailed = string.IsNullOrWhiteSpace(pfc?.Detailed) ? null : pfc.Detailed.Trim();

        var frequency = MapRecurringFrequency(stream.Frequency);
        var lastAmount = stream.LastAmount?.Amount;
        var averageAmount = stream.AverageAmount?.Amount;

        var firstDate = CoercePlaidStreamDate(stream.FirstDate);
        var lastDate = CoercePlaidStreamDate(stream.LastDate);
        var predictedNext = CoercePlaidStreamDate(stream.PredictedNextDate);

        var now = DateTimeOffset.UtcNow;
        var existing = await recurringRepository
            .GetByProfileAndPlaidStreamIdAsync(profileId, streamId, cancellationToken)
            .ConfigureAwait(false);

        if (existing is null)
        {
            var expected = averageAmount ?? lastAmount ?? 0m;
            var row = new ProfileRecurringCashflow
            {
                Id = Guid.NewGuid(),
                ProfileId = profileId,
                PlaidStreamId = streamId,
                LinkedBankAccountId = appStatus == "unlinked" ? null : linkedId,
                Direction = direction,
                MerchantName = merchantName,
                Description = description,
                PfcPrimary = pfcPrimary,
                PfcDetailed = pfcDetailed,
                Frequency = frequency,
                LastAmount = lastAmount,
                ExpectedAmount = expected,
                ExpectedAmountUserSet = false,
                FirstDate = firstDate,
                LastDate = lastDate,
                PredictedNextDate = predictedNext,
                Status = appStatus,
                CreatedAt = now,
                UpdatedAt = now
            };

            await recurringRepository.InsertAsync(row, cancellationToken).ConfigureAwait(false);
            return;
        }

        existing.LinkedBankAccountId = appStatus == "unlinked" ? null : linkedId;
        existing.Direction = direction;
        existing.MerchantName = merchantName;
        existing.Description = description;
        existing.PfcPrimary = pfcPrimary;
        existing.PfcDetailed = pfcDetailed;
        existing.Frequency = frequency;
        existing.LastAmount = lastAmount;
        existing.FirstDate = firstDate;
        existing.LastDate = lastDate;
        existing.PredictedNextDate = predictedNext;
        existing.Status = appStatus;

        if (!existing.ExpectedAmountUserSet)
        {
            var expected = averageAmount ?? lastAmount ?? existing.ExpectedAmount;
            existing.ExpectedAmount = expected;
        }

        existing.UpdatedAt = now;
        await recurringRepository.UpdateAsync(existing, cancellationToken).ConfigureAwait(false);
    }

    private static string MapAppStatus(TransactionStream stream, Guid? linkedAccountId, bool accountActive)
    {
        if (linkedAccountId is null || !accountActive)
            return "unlinked";

        if (stream.Status == TransactionStreamStatus.Tombstoned)
            return "inactive";

        return !stream.IsActive ? "inactive" : "active";
    }

    private static DateOnly? CoercePlaidStreamDate(object? value)
    {
        switch (value)
        {
            case null:
                return null;
            case DateOnly d:
                return d;
            case DateTime dt:
            {
                if (dt.Kind == DateTimeKind.Unspecified)
                    return DateOnly.FromDateTime(dt.Date);
                var utc = dt.Kind == DateTimeKind.Utc ? dt : dt.ToUniversalTime();
                return DateOnly.FromDateTime(utc.Date);
            }
            default:
                return null;
        }
    }

    private static string MapRecurringFrequency(RecurringTransactionFrequency? frequency)
    {
        return frequency switch
        {
            RecurringTransactionFrequency.Weekly => "WEEKLY",
            RecurringTransactionFrequency.Biweekly => "BIWEEKLY",
            RecurringTransactionFrequency.SemiMonthly => "SEMI_MONTHLY",
            RecurringTransactionFrequency.Monthly => "MONTHLY",
            RecurringTransactionFrequency.Annually => "ANNUALLY",
            RecurringTransactionFrequency.Unknown => "UNKNOWN",
            RecurringTransactionFrequency.Undefined => "UNKNOWN",
            null => "UNKNOWN",
            _ => "UNKNOWN"
        };
    }
}
