using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace finance_tracker_backend.Models;

[Table("push_logs")]
public class PushLog : BaseModel
{
    [PrimaryKey("id", shouldInsert: true)]
    public Guid Id { get; set; }

    [Column("profile_id")]
    public Guid? ProfileId { get; set; }

    [Column("push_subscription_id")]
    public Guid? PushSubscriptionId { get; set; }

    [Column("dedupe_key")]
    public string? DedupeKey { get; set; }

    [Column("status")]
    public string Status { get; set; } = string.Empty;

    [Column("provider_message_id")]
    public string? ProviderMessageId { get; set; }

    [Column("error_message")]
    public string? ErrorMessage { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }
}
