using System.Security.Claims;
using finance_tracker_backend.Contracts.Requests;
using finance_tracker_backend.Contracts.Responses;

namespace finance_tracker_backend.Services;

public interface IGroupedExpensesByAccountService
{
    Task<GroupedExpensesByAccountResponse> GetAsync(
        ClaimsPrincipal user,
        GroupedExpensesByAccountQueryRequest request,
        CancellationToken cancellationToken = default);
}
