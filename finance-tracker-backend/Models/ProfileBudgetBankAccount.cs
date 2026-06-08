using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace finance_tracker_backend.Models;

[Table("profile_budget_bank_accounts")]
public class ProfileBudgetBankAccount : BaseModel
{
    [PrimaryKey("id", shouldInsert: true)]
    public Guid Id { get; set; }

    [Column("budget_id")]
    public Guid BudgetId { get; set; }

    [Column("linked_bank_account_id")]
    public Guid LinkedBankAccountId { get; set; }
}
