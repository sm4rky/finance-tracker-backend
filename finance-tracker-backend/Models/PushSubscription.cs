using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace finance_tracker_backend.Models;

[Table("push_subscriptions")]
public class PushSubscription : BaseModel
{
    [PrimaryKey("id", shouldInsert: true)]
    public Guid Id { get; set; }

    [Column("profile_id")]
    public Guid ProfileId { get; set; }

    [Column("endpoint")]
    public string Endpoint { get; set; } = string.Empty;

    [Column("p256dh")]
    public string P256dh { get; set; } = string.Empty;

    [Column("auth")]
    public string Auth { get; set; } = string.Empty;

    [Column("user_agent")]
    public string? UserAgent { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }
}
