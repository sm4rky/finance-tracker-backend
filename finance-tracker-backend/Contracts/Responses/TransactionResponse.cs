namespace finance_tracker_backend.Contracts.Responses;

public sealed class TransactionResponse
{
    public Guid Id { get; init; }
    public Guid? LinkedBankAccountId { get; init; }
    public string PlaidTransactionId { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string? IsoCurrencyCode { get; init; }
    public DateOnly Date { get; init; }
    public DateOnly? AuthorizedDate { get; init; }
    public DateTimeOffset? AuthorizedDatetime { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? MerchantName { get; init; }
    public bool Pending { get; init; }
    public string? PaymentChannel { get; init; }
    public string? PfcPrimary { get; init; }
    public string? PfcDetailed { get; init; }
    public string? LogoUrl { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTimeOffset? RemovedAt { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}
