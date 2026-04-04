using System.Security.Claims;
using finance_tracker_backend.Contracts.Requests;
using finance_tracker_backend.Contracts.Responses;
using finance_tracker_backend.Infrastructure;
using finance_tracker_backend.Models;
using finance_tracker_backend.Repositories;

namespace finance_tracker_backend.Services;

public sealed class ProfileRecurringCashflowService(
    IProfileRecurringCashflowRepository recurringRepository,
    ILinkedBankAccountRepository linkedBankAccountRepository,
    ILinkedBankRepository linkedBankRepository) : IProfileRecurringCashflowService
{
    private static readonly HashSet<string> Directions = new(StringComparer.OrdinalIgnoreCase)
    {
        "inflow",
        "outflow"
    };

    private static readonly HashSet<string> Frequencies = new(StringComparer.OrdinalIgnoreCase)
    {
        "UNKNOWN",
        "WEEKLY",
        "BIWEEKLY",
        "SEMI_MONTHLY",
        "MONTHLY",
        "ANNUALLY",
        "ONE_TIME"
    };

    private static readonly HashSet<string> Statuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "active",
        "inactive",
        "unlinked"
    };

    public async Task<IReadOnlyList<ProfileRecurringCashflowResponse>> ListAsync(
        ClaimsPrincipal user,
        string? status,
        CancellationToken cancellationToken = default)
    {
        var profileId = user.RequireProfileId();
        if (status is not null && !Statuses.Contains(status.Trim()))
            throw new ArgumentException("status must be 'active', 'inactive', or 'unlinked'.");

        var rows = await recurringRepository
            .ListForProfileAsync(profileId, string.IsNullOrWhiteSpace(status) ? null : status.Trim(), cancellationToken)
            .ConfigureAwait(false);

        return await MapRowsToResponsesAsync(profileId, rows, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ProfileRecurringCashflowResponse> GetByIdAsync(
        ClaimsPrincipal user,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var profileId = user.RequireProfileId();
        var row = await recurringRepository
            .GetByIdForProfileAsync(profileId, id, cancellationToken)
            .ConfigureAwait(false);

        if (row is null)
            throw new KeyNotFoundException("Recurring cashflow was not found.");

        var map = await LoadAccountMapAsync(
                profileId,
                row.LinkedBankAccountId is { } lid ? [lid] : [],
                cancellationToken)
            .ConfigureAwait(false);
        return MapRow(row, ResolveAccount(row.LinkedBankAccountId, map));
    }

    public async Task<ProfileRecurringCashflowResponse> CreateAsync(
        ClaimsPrincipal user,
        SaveProfileRecurringCashflowRequest request,
        CancellationToken cancellationToken = default)
    {
        var profileId = user.RequireProfileId();
        ValidateDirection(request.Direction);
        ValidateFrequency(request.Frequency);

        var linkedId = await EnsureLinkedBankAccountBelongsToProfileAsync(
                profileId,
                request.LinkedBankAccountId,
                cancellationToken)
            .ConfigureAwait(false);

        var now = DateTimeOffset.UtcNow;
        var row = new ProfileRecurringCashflow
        {
            Id = Guid.NewGuid(),
            ProfileId = profileId,
            PlaidStreamId = null,
            LinkedBankAccountId = linkedId,
            Direction = request.Direction.Trim().ToLowerInvariant(),
            MerchantName = TrimToNull(request.MerchantName),
            Description = TrimToNull(request.Description),
            PfcPrimary = TrimToNull(request.PfcPrimary),
            PfcDetailed = TrimToNull(request.PfcDetailed),
            Frequency = request.Frequency.Trim().ToUpperInvariant(),
            LastAmount = request.LastAmount,
            ExpectedAmount = request.ExpectedAmount,
            ExpectedAmountUserSet = true,
            FirstDate = request.FirstDate,
            LastDate = request.LastDate,
            PredictedNextDate = request.PredictedNextDate,
            Status = ResolveStatusForManualSave(linkedId, existingRow: null),
            CreatedAt = now,
            UpdatedAt = now
        };

        await recurringRepository.InsertAsync(row, cancellationToken).ConfigureAwait(false);

        var map = await LoadAccountMapAsync(
                profileId,
                row.LinkedBankAccountId is { } x ? [x] : [],
                cancellationToken)
            .ConfigureAwait(false);
        return MapRow(row, ResolveAccount(row.LinkedBankAccountId, map));
    }

    public async Task<ProfileRecurringCashflowResponse> UpdateAsync(
        ClaimsPrincipal user,
        Guid id,
        SaveProfileRecurringCashflowRequest request,
        CancellationToken cancellationToken = default)
    {
        var profileId = user.RequireProfileId();
        var existing = await recurringRepository
            .GetByIdForProfileAsync(profileId, id, cancellationToken)
            .ConfigureAwait(false);

        if (existing is null)
            throw new KeyNotFoundException("Recurring cashflow was not found.");

        ValidateDirection(request.Direction);
        ValidateFrequency(request.Frequency);

        var linkedId = await EnsureLinkedBankAccountBelongsToProfileAsync(
                profileId,
                request.LinkedBankAccountId,
                cancellationToken)
            .ConfigureAwait(false);

        var expectedChanged = existing.ExpectedAmount != request.ExpectedAmount;
        existing.LinkedBankAccountId = linkedId;
        existing.Direction = request.Direction.Trim().ToLowerInvariant();
        existing.MerchantName = TrimToNull(request.MerchantName);
        existing.Description = TrimToNull(request.Description);
        existing.PfcPrimary = TrimToNull(request.PfcPrimary);
        existing.PfcDetailed = TrimToNull(request.PfcDetailed);
        existing.Frequency = request.Frequency.Trim().ToUpperInvariant();
        existing.LastAmount = request.LastAmount;
        existing.ExpectedAmount = request.ExpectedAmount;
        if (expectedChanged)
            existing.ExpectedAmountUserSet = true;
        existing.FirstDate = request.FirstDate;
        existing.LastDate = request.LastDate;
        existing.PredictedNextDate = request.PredictedNextDate;
        existing.Status = ResolveStatusForManualSave(linkedId, existing);
        existing.UpdatedAt = DateTimeOffset.UtcNow;

        await recurringRepository.UpdateAsync(existing, cancellationToken).ConfigureAwait(false);

        var map = await LoadAccountMapAsync(
                profileId,
                existing.LinkedBankAccountId is { } x ? [x] : [],
                cancellationToken)
            .ConfigureAwait(false);
        return MapRow(existing, ResolveAccount(existing.LinkedBankAccountId, map));
    }

    public async Task DeleteAsync(ClaimsPrincipal user, Guid id, CancellationToken cancellationToken = default)
    {
        var profileId = user.RequireProfileId();
        var n = await recurringRepository.DeleteForProfileAsync(profileId, id, cancellationToken).ConfigureAwait(false);
        if (n == 0)
            throw new KeyNotFoundException("Recurring cashflow was not found.");
    }

    public async Task<IReadOnlyList<ProfileRecurringCashflowCalendarOccurrenceResponse>> GetCalendarAsync(
        ClaimsPrincipal user,
        DateOnly dateFrom,
        DateOnly dateTo,
        CancellationToken cancellationToken = default)
    {
        if (dateFrom > dateTo)
            throw new ArgumentException("dateFrom must be on or before dateTo.");

        const int maxSpan = 800;
        if (dateTo.DayNumber - dateFrom.DayNumber > maxSpan)
            throw new ArgumentException($"Date range cannot exceed {maxSpan} days.");

        var profileId = user.RequireProfileId();
        var profileRecurringCashflows = await recurringRepository
            .ListForProfileAsync(profileId, null, cancellationToken)
            .ConfigureAwait(false);

        var activeOrUnlinked = profileRecurringCashflows
            .Where(r => r.Status is "active" or "unlinked")
            .ToList();

        var accountIds = activeOrUnlinked
            .Select(r => r.LinkedBankAccountId)
            .Where(x => x is not null)
            .Cast<Guid>()
            .Distinct()
            .ToList();

        var accountMap = await LoadAccountMapAsync(profileId, accountIds, cancellationToken).ConfigureAwait(false);

        var occurrences = new List<ProfileRecurringCashflowCalendarOccurrenceResponse>();
        foreach (var profileRecurringCashflow in activeOrUnlinked)
        {
            RecurringCashflowLinkedBankAccountResponse? linked = null;
            if (profileRecurringCashflow.LinkedBankAccountId is { } lid && accountMap.TryGetValue(lid, out var acc))
                linked = ToLinkedBankAccountResponse(acc);

            occurrences.AddRange(
                RecurringCashflowCalendarGenerator.ExpandRow(profileRecurringCashflow, dateFrom, dateTo, linked));
        }

        return occurrences
            .OrderBy(o => o.Date)
            .ThenBy(o => o.RecurringCashflowId)
            .ToList();
    }

    private async Task<IReadOnlyList<ProfileRecurringCashflowResponse>> MapRowsToResponsesAsync(
        Guid profileId,
        IReadOnlyList<ProfileRecurringCashflow> rows,
        CancellationToken cancellationToken)
    {
        var ids = rows
            .Select(r => r.LinkedBankAccountId)
            .Where(x => x is not null)
            .Cast<Guid>()
            .Distinct()
            .ToList();

        var map = await LoadAccountMapAsync(profileId, ids, cancellationToken).ConfigureAwait(false);
        return rows
            .Select(r => MapRow(r, ResolveAccount(r.LinkedBankAccountId, map)))
            .ToList();
    }

    private static LinkedBankAccount? ResolveAccount(Guid? linkedBankAccountId, Dictionary<Guid, LinkedBankAccount> map)
    {
        return linkedBankAccountId is null ? null : map.GetValueOrDefault(linkedBankAccountId.Value);
    }

    private async Task<Dictionary<Guid, LinkedBankAccount>> LoadAccountMapAsync(
        Guid profileId,
        IReadOnlyCollection<Guid> accountIds,
        CancellationToken cancellationToken)
    {
        var map = new Dictionary<Guid, LinkedBankAccount>();
        foreach (var id in accountIds.Distinct())
        {
            var linkedBankAccount = await linkedBankAccountRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
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

    private static ProfileRecurringCashflowResponse MapRow(
        ProfileRecurringCashflow row,
        LinkedBankAccount? account) => new()
    {
        Id = row.Id,
        MerchantName = row.MerchantName,
        Description = row.Description,
        PfcPrimary = row.PfcPrimary,
        PfcDetailed = row.PfcDetailed,
        Direction = row.Direction,
        Frequency = row.Frequency,
        Status = row.Status,
        LastAmount = row.LastAmount,
        ExpectedAmount = row.ExpectedAmount,
        ExpectedAmountUserSet = row.ExpectedAmountUserSet,
        FirstDate = row.FirstDate,
        LastDate = row.LastDate,
        PredictedNextDate = row.PredictedNextDate,
        PlaidStreamId = row.PlaidStreamId,
        LinkedBankAccount = ToLinkedBankAccountResponse(account)
    };

    private static RecurringCashflowLinkedBankAccountResponse? ToLinkedBankAccountResponse(LinkedBankAccount? a)
    {
        if (a is null)
            return null;

        return new RecurringCashflowLinkedBankAccountResponse
        {
            Id = a.Id,
            Name = a.AccountName ?? a.OfficialName ?? "Account",
            Mask = a.Mask,
            Type = a.Type,
            Subtype = a.Subtype
        };
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

        return bank is null ? throw new ArgumentException("Linked bank account does not belong to your profile.") : account.Id;
    }

    private static void ValidateDirection(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw) || !Directions.Contains(raw.Trim()))
            throw new ArgumentException("direction must be 'inflow' or 'outflow'.");
    }

    private static void ValidateFrequency(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw) || !Frequencies.Contains(raw.Trim()))
            throw new ArgumentException("Invalid frequency.");
    }

    private static string ResolveStatusForManualSave(Guid? linkedBankAccountId, ProfileRecurringCashflow? existingRow)
    {
        if (linkedBankAccountId is null)
            return "unlinked";

        if (existingRow is not null &&
            !string.IsNullOrEmpty(existingRow.PlaidStreamId) &&
            string.Equals(existingRow.Status, "inactive", StringComparison.OrdinalIgnoreCase))
            return "inactive";

        return "active";
    }

    private static string? TrimToNull(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
