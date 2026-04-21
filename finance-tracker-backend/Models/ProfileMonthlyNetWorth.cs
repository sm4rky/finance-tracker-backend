using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace finance_tracker_backend.Models;

[Table("profile_monthly_net_worth")]
public class ProfileMonthlyNetWorth : BaseModel
{
    [PrimaryKey("id", shouldInsert: true)]
    public Guid Id { get; set; }

    [Column("profile_id")]
    public Guid ProfileId { get; set; }

    /// <summary>First day of the calendar month this row represents (UTC date).</summary>
    [Column("period_start_date")]
    public DateOnly PeriodStartDate { get; set; }

    [Column("total_assets")]
    public decimal TotalAssets { get; set; }

    [Column("total_liabilities")]
    public decimal TotalLiabilities { get; set; }

    [Column("net_worth")]
    public decimal NetWorth { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }
}
