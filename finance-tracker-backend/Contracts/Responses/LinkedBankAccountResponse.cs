namespace finance_tracker_backend.Contracts.Responses;

public sealed class LinkedBankAccountResponse
{
    public Guid Id { get; set; }
    public Guid LinkedBankId { get; set; }
    public string PlaidAccountId { get; set; } = string.Empty;
    public string? AccountName { get; set; }
    public string? OfficialName { get; set; }
    public string? Mask { get; set; }
    public string? Type { get; set; }
    public string? Subtype { get; set; }
    public decimal? CurrentBalance { get; set; }
    public decimal? AvailableBalance { get; set; }
    public decimal? LimitAmount { get; set; }
    public string? IsoCurrencyCode { get; set; }
    public string? UnofficialCurrencyCode { get; set; }
    public DateTimeOffset? BalanceLastFetchedAt { get; set; }
    public bool IsActive { get; set; }
}
