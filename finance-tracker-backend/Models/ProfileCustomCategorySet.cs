using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace finance_tracker_backend.Models;

[Table("profile_custom_category_set")]
public class ProfileCustomCategorySet : BaseModel
{
    [PrimaryKey("id", shouldInsert: true)]
    public Guid Id { get; set; }

    [Column("profile_id")]
    public Guid ProfileId { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }
}
