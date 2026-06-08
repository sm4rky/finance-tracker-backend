using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace finance_tracker_backend.Models;

[Table("profile_budget_periods")]
public class ProfileBudgetPeriod : BaseModel
{
    [PrimaryKey("id", shouldInsert: true)]
    public Guid Id { get; set; }

    [Column("budget_id")]
    public Guid BudgetId { get; set; }

    [Column("period_start_date")]
    public DateOnly PeriodStartDate { get; set; }

    [Column("period_end_date")]
    public DateOnly PeriodEndDate { get; set; }

    [Column("period_name")]
    public string PeriodName { get; set; } = string.Empty;

    [Column("amount_limit")]
    public decimal AmountLimit { get; set; }

    [Column("spent_amount")]
    public decimal SpentAmount { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }
}
