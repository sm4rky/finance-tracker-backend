using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace finance_tracker_backend.Models;

[Table("profile_recurring_cashflow")]
public class ProfileRecurringCashflow : BaseModel
{
    [PrimaryKey("id", shouldInsert: true)]
    public Guid Id { get; set; }

    [Column("profile_id")]
    public Guid ProfileId { get; set; }

    [Column("plaid_stream_id")]
    public string? PlaidStreamId { get; set; }

    [Column("linked_bank_account_id")]
    public Guid? LinkedBankAccountId { get; set; }

    [Column("direction")]
    public string Direction { get; set; } = string.Empty;

    [Column("merchant_name")]
    public string? MerchantName { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [Column("pfc_primary")]
    public string? PfcPrimary { get; set; }

    [Column("pfc_detailed")]
    public string? PfcDetailed { get; set; }

    [Column("frequency")]
    public string Frequency { get; set; } = string.Empty;

    [Column("last_amount")]
    public decimal? LastAmount { get; set; }

    [Column("expected_amount")]
    public decimal ExpectedAmount { get; set; }

    [Column("expected_amount_user_set")]
    public bool ExpectedAmountUserSet { get; set; }

    [Column("first_date")]
    public DateOnly? FirstDate { get; set; }

    [Column("last_date")]
    public DateOnly? LastDate { get; set; }

    [Column("predicted_next_date")]
    public DateOnly? PredictedNextDate { get; set; }

    [Column("status")]
    public string Status { get; set; } = "active";

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }
}
