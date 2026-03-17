using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace finance_tracker_backend.Models;

[Table("subscription_plans")]
public class SubscriptionPlan : BaseModel
{
    [PrimaryKey("id")]
    public string Id { get; set; } = string.Empty;

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("max_accounts")]
    public int? MaxAccounts { get; set; }

    [Column("history_months")]
    public int? HistoryMonths { get; set; }

    [Column("has_ai")]
    public bool HasAi { get; set; }

    [Column("monthly_price")]
    public decimal? MonthlyPrice { get; set; }
}
