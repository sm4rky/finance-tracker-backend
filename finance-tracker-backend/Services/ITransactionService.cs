using System.Security.Claims;
using finance_tracker_backend.Contracts.Pagination;
using finance_tracker_backend.Contracts.Requests;
using finance_tracker_backend.Contracts.Responses;

namespace finance_tracker_backend.Services;

public interface ITransactionService
{
    Task<PagedResponse<TransactionResponse>> QueryAsync(
        ClaimsPrincipal user,
        QueryTransactionsRequest request,
        CancellationToken cancellationToken = default);
}
