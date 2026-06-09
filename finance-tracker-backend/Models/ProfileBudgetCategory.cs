using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace finance_tracker_backend.Models;

[Table("profile_budget_categories")]
public class ProfileBudgetCategory : BaseModel
{
    [PrimaryKey("id", shouldInsert: true)]
    public Guid Id { get; set; }

    [Column("budget_id")]
    public Guid BudgetId { get; set; }

    [Column("pfc_primary_code")]
    public string? PfcPrimaryCode { get; set; }

    [Column("pfc_version")]
    public string? PfcVersion { get; set; }

    [Column("custom_category_id")]
    public Guid? CustomCategoryId { get; set; }
}
