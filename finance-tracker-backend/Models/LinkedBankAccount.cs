using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace finance_tracker_backend.Models;

[Table("linked_bank_accounts")]
public class LinkedBankAccount : BaseModel
{
    [PrimaryKey("id", shouldInsert: true)]
    public Guid Id { get; set; }

    [Column("linked_bank_id")]
    public Guid LinkedBankId { get; set; }

    [Column("plaid_account_id")]
    public string PlaidAccountId { get; set; } = string.Empty;

    [Column("account_name")]
    public string? AccountName { get; set; }

    [Column("official_name")]
    public string? OfficialName { get; set; }

    [Column("mask")]
    public string? Mask { get; set; }

    [Column("type")]
    public string? Type { get; set; }

    [Column("subtype")]
    public string? Subtype { get; set; }

    [Column("current_balance")]
    public decimal? CurrentBalance { get; set; }

    [Column("available_balance")]
    public decimal? AvailableBalance { get; set; }

    [Column("limit_amount")]
    public decimal? LimitAmount { get; set; }

    [Column("iso_currency_code")]
    public string? IsoCurrencyCode { get; set; }

    [Column("unofficial_currency_code")]
    public string? UnofficialCurrencyCode { get; set; }

    [Column("balance_last_fetched_at")]
    public DateTimeOffset? BalanceLastFetchedAt { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }
}
