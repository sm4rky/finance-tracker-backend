using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace finance_tracker_backend.Models;

[Table("subscription_payments")]
public class SubscriptionPayment : BaseModel
{
    [PrimaryKey("id")]
    public Guid Id { get; set; }

    [Column("profile_id")]
    public Guid? ProfileId { get; set; }

    [Column("amount")]
    public decimal Amount { get; set; }

    [Column("charged_at")]
    public DateTimeOffset ChargedAt { get; set; }

    [Column("plan_id")]
    public string PlanId { get; set; } = string.Empty;

    [Column("payment_type")]
    public string PaymentType { get; set; } = string.Empty;

    [Column("reference")]
    public string? Reference { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }
}
