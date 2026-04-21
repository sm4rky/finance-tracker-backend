using System.Globalization;
using System.Security.Claims;
using finance_tracker_backend.Contracts.Requests;
using finance_tracker_backend.Contracts.Responses;
using finance_tracker_backend.Infrastructure;
using finance_tracker_backend.Repositories;

namespace finance_tracker_backend.Services;

public sealed class NetWorthService(
    ILinkedBankRepository linkedBankRepository,
    ILinkedBankAccountRepository linkedBankAccountRepository,
    IProfileMonthlyNetWorthRepository profileMonthlyNetWorthRepository) : INetWorthService
{
    public Task<NetWorthResponse> GetNetWorthAsync(
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default) =>
        GetNetWorthForProfileAsync(user.RequireProfileId(), cancellationToken);

    public async Task<NetWorthResponse> GetNetWorthForProfileAsync(
        Guid profileId,
        CancellationToken cancellationToken = default)
    {
        var banks = await linkedBankRepository.ListByProfileIdAsync(profileId, cancellationToken).ConfigureAwait(false);
        if (banks.Count == 0)
        {
            return new NetWorthResponse();
        }

        var bankIds = banks.Select(b => b.Id).ToList();
        var accounts = await linkedBankAccountRepository.ListByLinkedBankIdsAsync(bankIds, cancellationToken)
            .ConfigureAwait(false);

        decimal assets = 0m;
        decimal liabilities = 0m;
        foreach (var account in accounts.Where(a => a.IsActive))
        {
            var balance = account.CurrentBalance ?? 0m;
            var type = account.Type?.Trim();
            if (string.IsNullOrWhiteSpace(type))
                continue;

            switch (type.ToLowerInvariant())
            {
                case "depository":
                case "investment":
                case "brokerage":
                    assets += balance;
                    break;
                case "credit":
                case "loan":
                    liabilities += balance;
                    break;
                case "other":
                default:
                    break;
            }
        }

        return new NetWorthResponse
        {
            TotalAssets = assets,
            TotalLiabilities = liabilities,
            NetWorth = assets - liabilities
        };
    }

    public async Task<MonthlyNetWorthHistoryResponse> GetMonthlyNetWorthHistoryAsync(
        ClaimsPrincipal user,
        MonthlyNetWorthHistoryQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var profileId = user.RequireProfileId();

        DateOnly? from = null;
        DateOnly? to = null;

        if (!string.IsNullOrWhiteSpace(request.DateFrom))
        {
            if (!DateOnly.TryParse(
                    request.DateFrom.Trim(),
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var parsedFrom))
                throw new ArgumentException("dateFrom must be an ISO date (YYYY-MM-DD).");

            from = parsedFrom;
        }

        if (!string.IsNullOrWhiteSpace(request.DateTo))
        {
            if (!DateOnly.TryParse(
                    request.DateTo.Trim(),
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var parsedTo))
                throw new ArgumentException("dateTo must be an ISO date (YYYY-MM-DD).");

            to = parsedTo;
        }

        if (from is { } f && to is { } t && t < f)
            throw new ArgumentException("dateTo must be on or after dateFrom.");

        var rows = await profileMonthlyNetWorthRepository
            .ListByProfileIdAsync(profileId, from, to, cancellationToken)
            .ConfigureAwait(false);

        var items = rows
            .Select(r => new MonthlyNetWorthHistoryItemResponse
            {
                PeriodStartDate = r.PeriodStartDate,
                TotalAssets = r.TotalAssets,
                TotalLiabilities = r.TotalLiabilities,
                NetWorth = r.NetWorth,
                CreatedAt = r.CreatedAt
            })
            .ToList();

        return new MonthlyNetWorthHistoryResponse { Items = items };
    }
}
