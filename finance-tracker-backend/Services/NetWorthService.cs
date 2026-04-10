using System.Security.Claims;
using finance_tracker_backend.Contracts.Responses;
using finance_tracker_backend.Infrastructure;
using finance_tracker_backend.Repositories;

namespace finance_tracker_backend.Services;

public sealed class NetWorthService(
    ILinkedBankRepository linkedBankRepository,
    ILinkedBankAccountRepository linkedBankAccountRepository) : INetWorthService
{
    public async Task<NetWorthResponse> GetNetWorthAsync(
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        var profileId = user.RequireProfileId();
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
}
