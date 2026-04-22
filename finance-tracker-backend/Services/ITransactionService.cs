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

    Task<IReadOnlyList<TransactionResponse>> GetRecentAsync(
        ClaimsPrincipal user,
        int? limit = null,
        CancellationToken cancellationToken = default);

    Task<TransactionResponse> CreateAsync(
        ClaimsPrincipal user,
        SaveTransactionRequest request,
        CancellationToken cancellationToken = default);

    Task<TransactionResponse> UpdateAsync(
        ClaimsPrincipal user,
        Guid transactionId,
        SaveTransactionRequest request,
        CancellationToken cancellationToken = default);

    Task<DeleteTransactionsResponse> DeleteManyAsync(
        ClaimsPrincipal user,
        DeleteTransactionsRequest request,
        CancellationToken cancellationToken = default);
}
