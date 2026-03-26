using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace finance_tracker_backend.Models;

[Table("profile_subscriptions")]
public class ProfileSubscription : BaseModel
{
    [PrimaryKey("id", shouldInsert: true)]
    public Guid Id { get; set; }

    [Column("profile_id")]
    public Guid ProfileId { get; set; }

    [Column("plan_id")]
    public string PlanId { get; set; } = string.Empty;

    [Column("start_date")]
    public DateTimeOffset? StartDate { get; set; }

    [Column("end_date")]
    public DateTimeOffset? EndDate { get; set; }

    [Column("canceled_at")]
    public DateTimeOffset? CanceledAt { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }
}
