using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace finance_tracker_backend.Models;

[Table("email_logs")]
public class EmailLog : BaseModel
{
    [PrimaryKey("id", shouldInsert: true)]
    public Guid Id { get; set; }

    [Column("profile_id")]
    public Guid? ProfileId { get; set; }

    [Column("recipient_email")]
    public string RecipientEmail { get; set; } = string.Empty;

    [Column("template_id")]
    public string? TemplateId { get; set; }

    [Column("status")]
    public string Status { get; set; } = string.Empty;

    [Column("provider_message_id")]
    public string? ProviderMessageId { get; set; }

    [Column("error_message")]
    public string? ErrorMessage { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}
