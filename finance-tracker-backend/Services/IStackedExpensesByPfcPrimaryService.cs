using System.Security.Claims;
using finance_tracker_backend.Contracts.Requests;
using finance_tracker_backend.Contracts.Responses;

namespace finance_tracker_backend.Services;

public interface IStackedExpensesByCategoryService
{
    Task<StackedExpensesByCategoryResponse> GetAsync(
        ClaimsPrincipal user,
        StackedExpensesByCategoryQueryRequest request,
        CancellationToken cancellationToken = default);
}
