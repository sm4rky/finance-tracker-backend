using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace finance_tracker_backend.Models;

[Table("linked_banks")]
public class LinkedBank : BaseModel
{
    [PrimaryKey("id", shouldInsert: true)]
    public Guid Id { get; set; }

    [Column("profile_id")]
    public Guid ProfileId { get; set; }

    [Column("plaid_item_id")]
    public string PlaidItemId { get; set; } = string.Empty;

    [Column("plaid_access_token_encrypted")]
    public string? PlaidAccessTokenEncrypted { get; set; }

    [Column("institution_id")]
    public string? InstitutionId { get; set; }

    [Column("institution_name")]
    public string? InstitutionName { get; set; }

    [Column("status")]
    public string Status { get; set; } = "active";

    [Column("token_removed_at")]
    public DateTimeOffset? TokenRemovedAt { get; set; }

    [Column("disconnected_at")]
    public DateTimeOffset? DisconnectedAt { get; set; }

    [Column("last_synced_at")]
    public DateTimeOffset? LastSyncedAt { get; set; }

    [Column("plaid_transactions_cursor")]
    public string? PlaidTransactionsCursor { get; set; }

    [Column("pending_deselected_plaid_account_ids")]
    public string? PendingDeselectedPlaidAccountIds { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }
}
