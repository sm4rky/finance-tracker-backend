using System.Security.Claims;
using finance_tracker_backend.Contracts.Requests;
using finance_tracker_backend.Contracts.Responses;

namespace finance_tracker_backend.Services;

public interface IPlaidConnectionService
{
    Task<CreatePlaidLinkTokenResponse> CreateLinkTokenAsync(
        ClaimsPrincipal user,
        CreatePlaidLinkTokenRequest request,
        CancellationToken cancellationToken = default);

    Task<ExchangePlaidPublicTokenResponse> ExchangePublicTokenAsync(
        ClaimsPrincipal user,
        ExchangePlaidPublicTokenRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LinkedBankSummaryResponse>> ListConnectionsAsync(
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default);

    Task<SoftDisconnectLinkedBankResponse> SoftDisconnectAsync(
        ClaimsPrincipal user,
        Guid linkedBankId,
        CancellationToken cancellationToken = default);

    Task<HardDeleteLinkedBankResponse> HardDeleteAsync(
        ClaimsPrincipal user,
        Guid linkedBankId,
        CancellationToken cancellationToken = default);
}
