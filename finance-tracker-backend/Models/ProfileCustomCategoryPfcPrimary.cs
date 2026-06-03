using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace finance_tracker_backend.Models;

[Table("profile_custom_category_pfc_primary")]
public class ProfileCustomCategoryPfcPrimary : BaseModel
{
    [PrimaryKey("id", shouldInsert: true)]
    public Guid Id { get; set; }

    [Column("profile_custom_category_id")]
    public Guid ProfileCustomCategoryId { get; set; }

    [Column("pfc_primary_code")]
    public string PfcPrimaryCode { get; set; } = string.Empty;

    [Column("pfc_version")]
    public string PfcVersion { get; set; } = string.Empty;
}
