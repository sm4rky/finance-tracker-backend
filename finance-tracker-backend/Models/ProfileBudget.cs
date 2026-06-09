using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace finance_tracker_backend.Models;

[Table("profile_budgets")]
public class ProfileBudget : BaseModel
{
    [PrimaryKey("id", shouldInsert: true)]
    public Guid Id { get; set; }

    [Column("profile_id")]
    public Guid ProfileId { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("amount_limit")]
    public decimal AmountLimit { get; set; }

    [Column("is_recurring")]
    public bool IsRecurring { get; set; }

    [Column("period_type")]
    public string? PeriodType { get; set; }

    [Column("start_date")]
    public DateOnly StartDate { get; set; }

    [Column("end_date")]
    public DateOnly? EndDate { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("profile_custom_category_set_id")]
    public Guid? ProfileCustomCategorySetId { get; set; }

    [Column("include_income")]
    public bool IncludeIncome { get; set; }

    [Column("include_unlinked_transactions")]
    public bool IncludeUnlinkedTransactions { get; set; } = true;

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }
}
