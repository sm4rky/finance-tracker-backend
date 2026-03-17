using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace finance_tracker_backend.Models;

[Table("subscription_history")]
public class SubscriptionHistory : BaseModel
{
    [PrimaryKey("id")]
    public Guid Id { get; set; }

    [Column("profile_id")]
    public Guid? ProfileId { get; set; }

    [Column("event_type")]
    public string EventType { get; set; } = string.Empty;

    [Column("from_plan_id")]
    public string? FromPlanId { get; set; }

    [Column("to_plan_id")]
    public string? ToPlanId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("effective_at")]
    public DateTime EffectiveAt { get; set; }
}
