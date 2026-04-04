using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using finance_tracker_backend.Infrastructure;
using finance_tracker_backend.Contracts.Requests;
using finance_tracker_backend.Contracts.Responses;
using finance_tracker_backend.Models;
using finance_tracker_backend.Repositories;
using Going.Plaid;
using Going.Plaid.Accounts;
using Going.Plaid.Entity;
using Going.Plaid.Item;
using Going.Plaid.Link;
using Microsoft.Extensions.Configuration;
using Supabase.Postgrest.Exceptions;

namespace finance_tracker_backend.Services;

/// <summary>Plaid Link + Item flows (link token, exchange, linked banks/accounts). HTTP entry: <c>Controllers.PlaidController</c>.</summary>
public sealed class PlaidConnectionService(
    PlaidClient plaidClient,
    IConfiguration configuration,
    IPlaidLinkSessionRepository plaidLinkSessionRepository,
    ILinkedBankRepository linkedBankRepository,
    ILinkedBankAccountRepository linkedBankAccountRepository,
    ITransactionRepository transactionRepository,
    IProfileRecurringCashflowRepository profileRecurringCashflowRepository,
    PlaidAccessTokenProtector tokenProtector,
    IPlaidTransactionSyncService plaidTransactionSyncService,
    IPlaidRecurringCashflowRefreshService plaidRecurringCashflowRefreshService,
    ILogger<PlaidConnectionService> logger) : IPlaidConnectionService
{
    private readonly IConfigurationSection _plaid = configuration.GetSection("Plaid");

    // Recurring streams: same Transactions Item; /transactions/recurring/get — not a valid Link `products` entry.
    private static readonly Products[] DefaultProducts = [Products.Transactions];
    private static readonly CountryCode[] DefaultCountries = [CountryCode.Us];

    private const int MaxPlaidTransactionHistoryDaysRequested = 730;

    private const string TransactionStatusAccountOptedOut = "account_opted_out";

    public async Task<CreatePlaidLinkTokenResponse> CreateLinkTokenAsync(
        ClaimsPrincipal user,
        CreatePlaidLinkTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        var profileId = user.RequireProfileId();
        var intent = NormalizeIntent(request.Intent);

        if (intent is "relink" or "update")
        {
            if (request.LinkedBankId is null || request.LinkedBankId == Guid.Empty)
                throw new ArgumentException("LinkedBankId is required when intent is relink or update.");
        }

        var existingSession = await plaidLinkSessionRepository.GetByProfileAsync(profileId, cancellationToken)
            .ConfigureAwait(false);
        if (existingSession is not null && existingSession.ExpiresAt > DateTimeOffset.UtcNow)
        {
            return new CreatePlaidLinkTokenResponse
            {
                LinkToken = existingSession.LinkToken,
                LinkSessionId = existingSession.Id
            };
        }

        if (existingSession is not null && existingSession.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            await plaidLinkSessionRepository.DeleteByIdAndProfileAsync(existingSession.Id, profileId, cancellationToken)
                .ConfigureAwait(false);
            existingSession = null;
        }

        var linkRequest = await BuildLinkTokenCreateRequestAsync(profileId, intent, request.LinkedBankId, cancellationToken)
            .ConfigureAwait(false);
        var linkResponse = await plaidClient.LinkTokenCreateAsync(linkRequest).ConfigureAwait(false);
        if (!linkResponse.IsSuccessStatusCode)
        {
            var detail = linkResponse.Error?.ErrorMessage ?? linkResponse.Error?.ErrorCode ?? "Unknown Plaid error";
            logger.LogWarning("Plaid link/token/create failed: {Detail} (request_id={RequestId})", detail, linkResponse.RequestId);
            throw new InvalidOperationException($"Could not create Plaid Link session: {detail}");
        }

        var plaidExp = linkResponse.Expiration == default ? (DateTime?)null : linkResponse.Expiration.UtcDateTime;
        var expiresAt = ResolveLinkTokenExpiration(plaidExp, _plaid, intent);

        var sessionId = await SaveLinkSessionAsync(profileId, linkResponse.LinkToken, expiresAt, existingSession, intent, cancellationToken)
            .ConfigureAwait(false);

        return new CreatePlaidLinkTokenResponse { LinkToken = linkResponse.LinkToken, LinkSessionId = sessionId };
    }

    public async Task<ExchangePlaidPublicTokenResponse> ExchangePublicTokenAsync(
        ClaimsPrincipal user,
        ExchangePlaidPublicTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.PublicToken))
            throw new ArgumentException("PublicToken is required.");

        var profileId = user.RequireProfileId();
        var session = await plaidLinkSessionRepository.GetByIdAndProfileAsync(request.LinkSessionId, profileId, cancellationToken)
            .ConfigureAwait(false);

        if (session is null)
            throw new ArgumentException("Invalid or unknown link session.");

        if (session.ExpiresAt <= DateTimeOffset.UtcNow)
            throw new ArgumentException("This link session has expired. Request a new link token.");

        var exchange = await plaidClient.ItemPublicTokenExchangeAsync(new ItemPublicTokenExchangeRequest
        {
            PublicToken = request.PublicToken.Trim()
        }).ConfigureAwait(false);

        if (!exchange.IsSuccessStatusCode || string.IsNullOrWhiteSpace(exchange.AccessToken) || string.IsNullOrWhiteSpace(exchange.ItemId))
        {
            var detail = exchange.Error?.ErrorMessage ?? exchange.Error?.ErrorCode ?? "Unknown Plaid error";
            logger.LogWarning("Plaid item/public_token/exchange failed: {Detail}", detail);
            throw new InvalidOperationException("Could not complete bank connection. Try again.");
        }

        var accessToken = exchange.AccessToken!;
        var itemId = exchange.ItemId!;

        var accountsResponse = await plaidClient.AccountsGetAsync(new AccountsGetRequest
        {
            AccessToken = accessToken
        }).ConfigureAwait(false);

        if (!accountsResponse.IsSuccessStatusCode)
        {
            var detail = accountsResponse.Error?.ErrorMessage ?? accountsResponse.Error?.ErrorCode ?? "Unknown Plaid error";
            logger.LogWarning("Plaid accounts/get failed after exchange: {Detail}", detail);
            throw new InvalidOperationException("Connected, but account details could not be loaded.");
        }

        var institutionId = accountsResponse.Item?.InstitutionId;
        var institutionName = accountsResponse.Item?.InstitutionName;
        var now = DateTimeOffset.UtcNow;
        var encrypted = tokenProtector.Protect(accessToken);

        var existingBank = await linkedBankRepository.GetActiveByPlaidItemIdAsync(profileId, itemId, cancellationToken)
            .ConfigureAwait(false);

        LinkedBank bank;
        if (existingBank is not null)
        {
            existingBank.PlaidAccessTokenEncrypted = encrypted;
            existingBank.InstitutionId = institutionId;
            existingBank.InstitutionName = institutionName;
            existingBank.Status = "active";
            existingBank.TokenRemovedAt = null;
            existingBank.DisconnectedAt = null;
            existingBank.UpdatedAt = now;
            await linkedBankRepository.UpdateAsync(existingBank, cancellationToken).ConfigureAwait(false);
            bank = existingBank;
        }
        else
        {
            bank = new LinkedBank
            {
                Id = Guid.NewGuid(),
                ProfileId = profileId,
                PlaidItemId = itemId,
                PlaidAccessTokenEncrypted = encrypted,
                InstitutionId = institutionId,
                InstitutionName = institutionName,
                Status = "active",
                CreatedAt = now,
                UpdatedAt = now
            };
            await linkedBankRepository.InsertAsync(bank, cancellationToken).ConfigureAwait(false);
        }

        var sessionIntent = string.IsNullOrWhiteSpace(session.Intent) ? "connect" : session.Intent.Trim();
        var isUpdateIntent = string.Equals(sessionIntent, "update", StringComparison.OrdinalIgnoreCase);

        var accountDtos = await SyncAccountsFromPlaidAsync(
                bank.Id,
                accountsResponse.Accounts.ToList(),
                now,
                deferDeactivationForMissingPlaidAccounts: isUpdateIntent,
                cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<LinkedBankAccountResponse> pendingDeselectedDtos = [];
        var requiresAccountOptOutHandling = false;

        if (!isUpdateIntent)
            bank.PendingDeselectedPlaidAccountIds = null;
        else
        {
            var plaidIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var a in accountsResponse.Accounts)
            {
                if (!string.IsNullOrWhiteSpace(a.AccountId))
                    plaidIds.Add(a.AccountId);
            }

            var allDb = await linkedBankAccountRepository.ListByLinkedBankIdAsync(bank.Id, cancellationToken).ConfigureAwait(false);
            var deselectedRows = allDb.Where(x => x.IsActive && !plaidIds.Contains(x.PlaidAccountId)).ToList();
            requiresAccountOptOutHandling = deselectedRows.Count > 0;
            bank.PendingDeselectedPlaidAccountIds = deselectedRows.Count == 0
                ? null
                : JsonSerializer.Serialize(deselectedRows.Select(x => x.PlaidAccountId).ToList());
            pendingDeselectedDtos = deselectedRows.Select(MapAccount).ToList();
        }

        // LastSyncedAt is only for transaction-sync cooldown (set by PlaidTransactionSyncService). Clear it here so
        // post-exchange sync always runs (including banks that had LastSyncedAt set incorrectly on a previous version).
        bank.LastSyncedAt = null;
        bank.UpdatedAt = DateTimeOffset.UtcNow;
        await linkedBankRepository.UpdateAsync(bank, cancellationToken).ConfigureAwait(false);

        await plaidLinkSessionRepository.DeleteByIdAndProfileAsync(session.Id, profileId, cancellationToken).ConfigureAwait(false);

        try
        {
            // bypassCooldown: post-exchange sync must run even if last_synced_at could not be cleared in DB (e.g. relink).
            await plaidTransactionSyncService
                .SyncLinkedBankAsync(user, bank.Id, cancellationToken, bypassCooldown: true)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(
                ex,
                "Initial Plaid transaction sync after exchange failed (LinkedBankId={LinkedBankId}). Client can call sync again.",
                bank.Id);
        }

        try
        {
            await plaidRecurringCashflowRefreshService
                .SyncLinkedBankRecurringCashflowsAsync(user, bank.Id, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(
                ex,
                "Plaid recurring cashflow refresh after exchange failed (LinkedBankId={LinkedBankId}). Client can call refresh again.",
                bank.Id);
        }

        return new ExchangePlaidPublicTokenResponse
        {
            LinkedBankId = bank.Id,
            PlaidItemId = bank.PlaidItemId,
            InstitutionId = bank.InstitutionId,
            InstitutionName = bank.InstitutionName,
            Accounts = accountDtos,
            RequiresAccountOptOutHandling = requiresAccountOptOutHandling,
            PendingDeselectedAccounts = pendingDeselectedDtos
        };
    }

    public async Task<ConfirmPlaidUpdateAccountsResponse> ConfirmUpdateModeAccountDecisionsAsync(
        ClaimsPrincipal user,
        Guid linkedBankId,
        ConfirmPlaidUpdateAccountsRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var profileId = user.RequireProfileId();

        var bank = await linkedBankRepository.GetByIdForProfileAsync(linkedBankId, profileId, cancellationToken).ConfigureAwait(false);
        if (bank is null)
            throw new ArgumentException("Linked bank was not found.");

        if (string.IsNullOrWhiteSpace(bank.PendingDeselectedPlaidAccountIds))
            throw new ArgumentException("There is no pending account opt-out to confirm for this connection.");

        List<string> pendingIds;
        try
        {
            pendingIds = JsonSerializer.Deserialize<List<string>>(bank.PendingDeselectedPlaidAccountIds!) ?? [];
        }
        catch (JsonException ex)
        {
            throw new ArgumentException("Invalid pending state on linked bank.", ex);
        }

        if (pendingIds.Count == 0)
            throw new ArgumentException("There is no pending account opt-out to confirm for this connection.");

        var decisions = request.Decisions ?? [];
        var pendingSet = new HashSet<string>(pendingIds, StringComparer.Ordinal);
        var map = new Dictionary<string, PlaidAccountOptOutDecision>(StringComparer.Ordinal);

        foreach (var d in decisions)
        {
            var pid = d.PlaidAccountId?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(pid))
                throw new ArgumentException("Each decision must include a PlaidAccountId.");
            if (!pendingSet.Contains(pid))
                throw new ArgumentException($"Plaid account id is not pending opt-out: {pid}.");
            if (!map.TryAdd(pid, d))
                throw new ArgumentException($"Duplicate Plaid account id in decisions: {pid}.");
        }

        if (map.Count != pendingSet.Count)
            throw new ArgumentException($"Provide exactly one decision per pending account ({pendingSet.Count} required).");

        var now = DateTimeOffset.UtcNow;

        foreach (var pid in pendingIds)
        {
            var d = map[pid];

            var acc = await linkedBankAccountRepository
                .GetByLinkedBankAndPlaidAccountIdAsync(linkedBankId, pid, cancellationToken)
                .ConfigureAwait(false);
            if (acc is null)
                throw new ArgumentException($"Account {pid} is no longer linked to this bank.");

            await profileRecurringCashflowRepository
                .SetStatusForLinkedBankAccountAsync(profileId, acc.Id, "unlinked", cancellationToken)
                .ConfigureAwait(false);

            if (d.DeleteTransactions)
            {
                await transactionRepository
                    .DeleteByProfileAndLinkedBankAccountIdAsync(profileId, acc.Id, cancellationToken)
                    .ConfigureAwait(false);
                await linkedBankAccountRepository
                    .DeleteByIdAndLinkedBankIdAsync(acc.Id, linkedBankId, cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                await transactionRepository
                    .SetStatusByLinkedBankAccountIdAsync(profileId, acc.Id, TransactionStatusAccountOptedOut, cancellationToken)
                    .ConfigureAwait(false);
                acc.IsActive = false;
                acc.UpdatedAt = now;
                await linkedBankAccountRepository.UpdateAsync(acc, cancellationToken).ConfigureAwait(false);
            }
        }

        bank.PendingDeselectedPlaidAccountIds = null;
        bank.UpdatedAt = now;
        await linkedBankRepository.UpdateAsync(bank, cancellationToken).ConfigureAwait(false);

        var sync = await plaidTransactionSyncService
            .SyncLinkedBankAsync(user, linkedBankId, cancellationToken, bypassCooldown: true)
            .ConfigureAwait(false);

        try
        {
            await plaidRecurringCashflowRefreshService
                .SyncLinkedBankRecurringCashflowsAsync(user, linkedBankId, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(
                ex,
                "Plaid recurring cashflow refresh after confirm update accounts failed (LinkedBankId={LinkedBankId}).",
                linkedBankId);
        }

        return new ConfirmPlaidUpdateAccountsResponse { LinkedBankId = linkedBankId, Sync = sync };
    }

    public async Task<IReadOnlyList<LinkedBankSummaryResponse>> ListConnectionsAsync(
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        var profileId = user.RequireProfileId();
        var banks = await linkedBankRepository.ListByProfileIdAsync(profileId, cancellationToken).ConfigureAwait(false);
        var ordered = banks.OrderBy(b => b.CreatedAt).ToList();
        var ids = ordered.Select(b => b.Id).ToList();
        var accounts = await linkedBankAccountRepository.ListByLinkedBankIdsAsync(ids, cancellationToken).ConfigureAwait(false);
        var activeAccounts = accounts.Where(a => a.IsActive).ToList();
        var byBank = activeAccounts
            .GroupBy(a => a.LinkedBankId)
            .ToDictionary(g => g.Key, g => g.OrderBy(a => a.CreatedAt).Select(MapAccount).ToList());

        return ordered.Select(b => new LinkedBankSummaryResponse
        {
            Id = b.Id,
            PlaidItemId = b.PlaidItemId,
            InstitutionId = b.InstitutionId,
            InstitutionName = b.InstitutionName,
            Status = b.Status,
            DisconnectedAt = b.DisconnectedAt,
            TokenRemovedAt = b.TokenRemovedAt,
            LastSyncedAt = b.LastSyncedAt,
            CreatedAt = b.CreatedAt,
            HasPendingUpdateAccountDecisions = !string.IsNullOrWhiteSpace(b.PendingDeselectedPlaidAccountIds),
            Accounts = byBank.GetValueOrDefault(b.Id) ?? []
        }).ToList();
    }

    public async Task<SoftDisconnectLinkedBankResponse> SoftDisconnectAsync(
        ClaimsPrincipal user,
        Guid linkedBankId,
        CancellationToken cancellationToken = default)
    {
        var profileId = user.RequireProfileId();
        var bank = await linkedBankRepository.GetByIdForProfileAsync(linkedBankId, profileId, cancellationToken).ConfigureAwait(false);
        if (bank is null)
            throw new ArgumentException("Linked bank was not found.");

        await TryRemovePlaidItemAsync(bank, cancellationToken).ConfigureAwait(false);

        var now = DateTimeOffset.UtcNow;
        bank.PlaidAccessTokenEncrypted = null;
        bank.Status = "soft_deleted";
        bank.TokenRemovedAt = now;
        bank.DisconnectedAt = now;
        bank.UpdatedAt = now;
        await linkedBankRepository.UpdateAsync(bank, cancellationToken).ConfigureAwait(false);

        return new SoftDisconnectLinkedBankResponse
        {
            LinkedBankId = bank.Id,
            Status = bank.Status,
            TokenRemoved = true
        };
    }

    public async Task<UnlinkInstitutionResponse> UnlinkInstitutionAsync(
        ClaimsPrincipal user,
        UnlinkInstitutionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.LinkedBankId == Guid.Empty)
            throw new ArgumentException("LinkedBankId is required.");

        var profileId = user.RequireProfileId();
        var linkedBankId = request.LinkedBankId;

        var bank = await linkedBankRepository.GetByIdForProfileAsync(linkedBankId, profileId, cancellationToken).ConfigureAwait(false);
        if (bank is null)
            throw new ArgumentException("Linked bank was not found.");

        await TryRemovePlaidItemAsync(bank, cancellationToken).ConfigureAwait(false);

        var transactionsRemoved = 0;
        if (request.DeleteTransactions)
        {
            transactionsRemoved = await transactionRepository
                .DeleteByProfileAndLinkedBankIdAsync(profileId, linkedBankId, cancellationToken)
                .ConfigureAwait(false);
        }

        await linkedBankRepository.HardDeleteByIdForProfileAsync(linkedBankId, profileId, cancellationToken).ConfigureAwait(false);

        return new UnlinkInstitutionResponse
        {
            LinkedBankId = linkedBankId,
            Deleted = true,
            TransactionsRemoved = transactionsRemoved
        };
    }

    private async Task TryRemovePlaidItemAsync(LinkedBank bank, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(bank.PlaidAccessTokenEncrypted))
            return;

        try
        {
            var accessToken = tokenProtector.Unprotect(bank.PlaidAccessTokenEncrypted);
            var removeResponse = await plaidClient.ItemRemoveAsync(new ItemRemoveRequest { AccessToken = accessToken })
                .ConfigureAwait(false);
            if (!removeResponse.IsSuccessStatusCode)
            {
                var detail = removeResponse.Error?.ErrorMessage ?? removeResponse.Error?.ErrorCode ?? "unknown";
                logger.LogWarning("Plaid item/remove error: {Detail}", detail);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Plaid item/remove failed; continuing.");
        }
    }

    private async Task<LinkTokenCreateRequest> BuildLinkTokenCreateRequestAsync(
        Guid profileId,
        string intent,
        Guid? linkedBankId,
        CancellationToken cancellationToken)
    {
        var clientName = string.IsNullOrWhiteSpace(_plaid["ClientName"]) ? "MoneyInsight" : _plaid["ClientName"]!.Trim();
        var request = new LinkTokenCreateRequest
        {
            ClientName = clientName,
            User = new LinkTokenCreateRequestUser { ClientUserId = profileId.ToString("D", CultureInfo.InvariantCulture) },
            Products = DefaultProducts,
            CountryCodes = DefaultCountries,
            Language = ResolveLinkLanguage(_plaid),
            Transactions = new LinkTokenTransactions { DaysRequested = MaxPlaidTransactionHistoryDaysRequested }
        };

        if (!string.IsNullOrWhiteSpace(_plaid["Webhook"]))
            request.Webhook = _plaid["Webhook"]!.Trim();

        var redirectUri = _plaid["RedirectUri"]?.Trim();
        if (!string.IsNullOrWhiteSpace(redirectUri) &&
            redirectUri.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            request.RedirectUri = redirectUri;

        if (intent is not ("relink" or "update")) return request;
        var bank = await linkedBankRepository.GetByIdForProfileAsync(linkedBankId!.Value, profileId, cancellationToken)
            .ConfigureAwait(false);
        if (bank is null)
            throw new ArgumentException("Linked bank was not found.");
        if (string.IsNullOrWhiteSpace(bank.PlaidAccessTokenEncrypted))
            throw new InvalidOperationException(
                "This connection has no stored Plaid access. Use connect flow instead of relink/update.");

        var plain = tokenProtector.Unprotect(bank.PlaidAccessTokenEncrypted);
        request.AccessToken = plain;

        return request;
    }

    private static DateTimeOffset LinkSessionExpiresAtToOffset(DateTime utcDeadline)
    {
        var utc = utcDeadline.Kind switch
        {
            DateTimeKind.Utc => utcDeadline,
            DateTimeKind.Local => utcDeadline.ToUniversalTime(),
            _ => DateTime.SpecifyKind(utcDeadline, DateTimeKind.Utc)
        };
        return new DateTimeOffset(utc, TimeSpan.Zero);
    }

    private static Language ResolveLinkLanguage(IConfigurationSection plaid)
    {
        var raw = plaid["LinkLanguage"]?.Trim();
        if (string.IsNullOrWhiteSpace(raw))
            return Language.English;
        return Enum.TryParse<Language>(raw, ignoreCase: true, out var lang) ? lang : Language.English;
    }

    private static DateTime ResolveLinkTokenExpiration(DateTime? plaidExpiration, IConfigurationSection plaid, string intent)
    {
        if (plaidExpiration.HasValue)
            return DateTime.SpecifyKind(plaidExpiration.Value, DateTimeKind.Utc);

        var hours = int.TryParse(plaid["LinkTokenExpirationHours"], out var h) && h > 0 ? h : 4;
        return intent is "relink" or "update" ? DateTime.UtcNow.AddMinutes(25) : DateTime.UtcNow.AddHours(hours);
    }

    private async Task<Guid> SaveLinkSessionAsync(
        Guid profileId,
        string linkToken,
        DateTime expiresAt,
        PlaidLinkSession? existingRow,
        string intent,
        CancellationToken cancellationToken)
    {
        if (existingRow is not null)
        {
            await plaidLinkSessionRepository.UpdateTokensAsync(
                    existingRow.Id, profileId, linkToken, LinkSessionExpiresAtToOffset(expiresAt), intent, cancellationToken)
                .ConfigureAwait(false);
            return existingRow.Id;
        }

        var row = new PlaidLinkSession
        {
            Id = Guid.NewGuid(),
            ProfileId = profileId,
            LinkToken = linkToken,
            ExpiresAt = LinkSessionExpiresAtToOffset(expiresAt),
            Intent = intent,
            CreatedAt = DateTimeOffset.UtcNow
        };

        try
        {
            await plaidLinkSessionRepository.InsertAsync(row, cancellationToken).ConfigureAwait(false);
            return row.Id;
        }
        catch (PostgrestException ex) when (IsUniqueConstraintViolation(ex))
        {
            var raced = await plaidLinkSessionRepository.GetByProfileAsync(profileId, cancellationToken)
                .ConfigureAwait(false);
            if (raced is null)
                throw;

            await plaidLinkSessionRepository.UpdateTokensAsync(
                    raced.Id, profileId, linkToken, LinkSessionExpiresAtToOffset(expiresAt), intent, cancellationToken)
                .ConfigureAwait(false);
            return raced.Id;
        }
    }

    private async Task<IReadOnlyList<LinkedBankAccountResponse>> SyncAccountsFromPlaidAsync(
        Guid linkedBankId,
        IList<Account> plaidAccounts,
        DateTimeOffset now,
        bool deferDeactivationForMissingPlaidAccounts,
        CancellationToken cancellationToken)
    {
        var existing = (await linkedBankAccountRepository.ListByLinkedBankIdAsync(linkedBankId, cancellationToken).ConfigureAwait(false))
            .ToDictionary(a => a.PlaidAccountId, StringComparer.Ordinal);

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var dtos = new List<LinkedBankAccountResponse>();

        foreach (var acc in plaidAccounts)
        {
            var pid = acc.AccountId;
            if (string.IsNullOrWhiteSpace(pid))
                continue;
            seen.Add(pid);

            var type = acc.Type.ToString();
            var subtype = acc.Subtype?.ToString() ?? string.Empty;

            if (existing.TryGetValue(pid, out var row))
            {
                row.AccountName = acc.Name;
                row.OfficialName = acc.OfficialName;
                row.Mask = acc.Mask;
                row.Type = type;
                row.Subtype = subtype;
                row.IsActive = true;
                ApplyPlaidBalances(row, acc.Balances, now);
                row.UpdatedAt = now;
                await linkedBankAccountRepository.UpdateAsync(row, cancellationToken).ConfigureAwait(false);
                dtos.Add(MapAccount(row));
            }
            else
            {
                var mergeInto = FindInactiveAccountMergeCandidate(existing, acc, type, subtype);
                if (mergeInto is not null)
                {
                    var oldPlaidId = mergeInto.PlaidAccountId;
                    existing.Remove(oldPlaidId);
                    mergeInto.PlaidAccountId = pid;
                    mergeInto.AccountName = acc.Name;
                    mergeInto.OfficialName = acc.OfficialName;
                    mergeInto.Mask = acc.Mask;
                    mergeInto.Type = type;
                    mergeInto.Subtype = subtype;
                    mergeInto.IsActive = true;
                    ApplyPlaidBalances(mergeInto, acc.Balances, now);
                    mergeInto.UpdatedAt = now;
                    await linkedBankAccountRepository.UpdateAsync(mergeInto, cancellationToken).ConfigureAwait(false);
                    existing[pid] = mergeInto;
                    dtos.Add(MapAccount(mergeInto));
                }
                else
                {
                    var insert = new LinkedBankAccount
                    {
                        Id = Guid.NewGuid(),
                        LinkedBankId = linkedBankId,
                        PlaidAccountId = pid,
                        AccountName = acc.Name,
                        OfficialName = acc.OfficialName,
                        Mask = acc.Mask,
                        Type = type,
                        Subtype = subtype,
                        IsActive = true,
                        CreatedAt = now,
                        UpdatedAt = now
                    };
                    ApplyPlaidBalances(insert, acc.Balances, now);
                    await linkedBankAccountRepository.InsertAsync(insert, cancellationToken).ConfigureAwait(false);
                    dtos.Add(MapAccount(insert));
                }
            }
        }

        if (!deferDeactivationForMissingPlaidAccounts)
        {
            foreach (var row in from kv in existing where !seen.Contains(kv.Key) select kv.Value)
            {
                row.IsActive = false;
                row.UpdatedAt = now;
                await linkedBankAccountRepository.UpdateAsync(row, cancellationToken).ConfigureAwait(false);
            }
        }

        return dtos;
    }

    private static void ApplyPlaidBalances(LinkedBankAccount row, AccountBalance? balances, DateTimeOffset fetchedAtUtc)
    {
        if (balances is null)
        {
            row.CurrentBalance = null;
            row.AvailableBalance = null;
            row.LimitAmount = null;
            row.IsoCurrencyCode = null;
            row.UnofficialCurrencyCode = null;
            row.BalanceLastFetchedAt = fetchedAtUtc;
            return;
        }

        row.CurrentBalance = balances.Current;
        row.AvailableBalance = balances.Available;
        row.LimitAmount = balances.Limit;
        row.IsoCurrencyCode = string.IsNullOrWhiteSpace(balances.IsoCurrencyCode) ? null : balances.IsoCurrencyCode.Trim();
        row.UnofficialCurrencyCode = string.IsNullOrWhiteSpace(balances.UnofficialCurrencyCode)
            ? null
            : balances.UnofficialCurrencyCode.Trim();
        row.BalanceLastFetchedAt = balances.LastUpdatedDatetime ?? fetchedAtUtc;
    }

    private static LinkedBankAccountResponse MapAccount(LinkedBankAccount a) => new()
    {
        Id = a.Id,
        LinkedBankId = a.LinkedBankId,
        PlaidAccountId = a.PlaidAccountId,
        AccountName = a.AccountName,
        OfficialName = a.OfficialName,
        Mask = a.Mask,
        Type = a.Type,
        Subtype = a.Subtype,
        CurrentBalance = a.CurrentBalance,
        AvailableBalance = a.AvailableBalance,
        LimitAmount = a.LimitAmount,
        IsoCurrencyCode = a.IsoCurrencyCode,
        UnofficialCurrencyCode = a.UnofficialCurrencyCode,
        BalanceLastFetchedAt = a.BalanceLastFetchedAt,
        IsActive = a.IsActive
    };

    private static LinkedBankAccount? FindInactiveAccountMergeCandidate(
        Dictionary<string, LinkedBankAccount> existing,
        Account acc,
        string type,
        string subtype)
    {
        var label = NormalizePlaidAccountLabel(acc.Name, acc.OfficialName);
        var maskNorm = NormalizeMask(acc.Mask);

        LinkedBankAccount? sole = null;
        foreach (var row in existing.Values)
        {
            if (row.IsActive)
                continue;
            if (!string.Equals(row.Type, type, StringComparison.Ordinal))
                continue;
            if (!string.Equals(row.Subtype, subtype, StringComparison.Ordinal))
                continue;
            if (!MaskMatches(maskNorm, row.Mask))
                continue;
            var rowLabel = NormalizePlaidAccountLabel(row.AccountName, row.OfficialName);
            if (!string.Equals(rowLabel, label, StringComparison.OrdinalIgnoreCase))
                continue;
            if (sole is not null)
                return null;
            sole = row;
        }

        return sole;
    }

    private static string NormalizePlaidAccountLabel(string? name, string? officialName)
    {
        var s = string.IsNullOrWhiteSpace(name) ? null : name.Trim();
        if (!string.IsNullOrEmpty(s))
            return s;
        s = string.IsNullOrWhiteSpace(officialName) ? null : officialName.Trim();
        return s ?? string.Empty;
    }

    private static string? NormalizeMask(string? mask) =>
        string.IsNullOrWhiteSpace(mask) ? null : mask.Trim();

    private static bool MaskMatches(string? plaidMaskNormalized, string? rowMask)
    {
        var r = NormalizeMask(rowMask);
        return string.Equals(plaidMaskNormalized ?? string.Empty, r ?? string.Empty, StringComparison.Ordinal);
    }

    private static bool IsUniqueConstraintViolation(PostgrestException ex) =>
        ex.Message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase)
        || ex.Message.Contains("23505", StringComparison.OrdinalIgnoreCase);

    private static string NormalizeIntent(string? intent)
    {
        var v = (intent ?? "connect").Trim().ToLowerInvariant();
        return v is not ("connect" or "relink" or "update") ? throw new ArgumentException("Intent must be connect, relink, or update.") : v;
    }
}
