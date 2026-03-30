using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace finance_tracker_backend.Models;

[Table("transactions")]
public class Transaction : BaseModel
{
    [PrimaryKey("id", shouldInsert: true)]
    public Guid Id { get; set; }

    [Column("profile_id")]
    public Guid ProfileId { get; set; }

    [Column("linked_bank_account_id")]
    public Guid? LinkedBankAccountId { get; set; }

    [Column("plaid_transaction_id")]
    public string PlaidTransactionId { get; set; } = string.Empty;

    [Column("amount")]
    public decimal Amount { get; set; }

    [Column("iso_currency_code")]
    public string? IsoCurrencyCode { get; set; }

    [Column("date")]
    public DateOnly Date { get; set; }

    [Column("authorized_date")]
    public DateOnly? AuthorizedDate { get; set; }

    [Column("authorized_datetime")]
    public DateTimeOffset? AuthorizedDatetime { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("merchant_name")]
    public string? MerchantName { get; set; }

    [Column("merchant_entity_id")]
    public string? MerchantEntityId { get; set; }

    [Column("pending")]
    public bool Pending { get; set; }

    [Column("pending_transaction_id")]
    public string? PendingTransactionId { get; set; }

    [Column("payment_channel")]
    public string? PaymentChannel { get; set; }

    [Column("transaction_type")]
    public string? TransactionType { get; set; }

    [Column("pfc_primary")]
    public string? PfcPrimary { get; set; }

    [Column("pfc_detailed")]
    public string? PfcDetailed { get; set; }

    [Column("pfc_confidence_level")]
    public string? PfcConfidenceLevel { get; set; }

    [Column("pfc_version")]
    public string? PfcVersion { get; set; }

    [Column("logo_url")]
    public string? LogoUrl { get; set; }

    [Column("website")]
    public string? Website { get; set; }

    [Column("status")]
    public string Status { get; set; } = "active";

    [Column("removed_at")]
    public DateTimeOffset? RemovedAt { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }
}
