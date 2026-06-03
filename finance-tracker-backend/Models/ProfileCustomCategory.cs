using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace finance_tracker_backend.Models;

[Table("profile_custom_category")]
public class ProfileCustomCategory : BaseModel
{
    [PrimaryKey("id", shouldInsert: true)]
    public Guid Id { get; set; }

    [Column("profile_custom_category_set_id")]
    public Guid ProfileCustomCategorySetId { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("color_set")]
    public string ColorSet { get; set; } = string.Empty;

    [Column("icon_name")]
    public string IconName { get; set; } = string.Empty;
}
