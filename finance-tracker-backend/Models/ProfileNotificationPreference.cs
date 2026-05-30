using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace finance_tracker_backend.Models;

[Table("profile_notification_preferences")]
public class ProfileNotificationPreference : BaseModel
{
    [PrimaryKey("id", shouldInsert: true)]
    public Guid Id { get; set; }

    [Column("profile_id")]
    public Guid ProfileId { get; set; }

    [Column("email_enabled")]
    public bool EmailEnabled { get; set; }

    [Column("due_reminder_enabled")]
    public bool DueReminderEnabled { get; set; }

    [Column("reminder_days_before")]
    public int ReminderDaysBefore { get; set; }

    [Column("budget_alert_enabled")]
    public bool BudgetAlertEnabled { get; set; }

    [Column("budget_alert_threshold")]
    public int BudgetAlertThreshold { get; set; }

    [Column("monthly_statement_enabled")]
    public bool MonthlyStatementEnabled { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }
}
