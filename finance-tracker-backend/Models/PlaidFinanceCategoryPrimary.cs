using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace finance_tracker_backend.Models;

[Table("plaid_finance_category_primary")]
public class PlaidFinanceCategoryPrimary : BaseModel
{
    [PrimaryKey("pfc_version", shouldInsert: false)]
    [Column("pfc_version")]
    public string PfcVersion { get; set; } = string.Empty;

    [PrimaryKey("code", shouldInsert: false)]
    [Column("code")]
    public string Code { get; set; } = string.Empty;

    [Column("display_name")]
    public string DisplayName { get; set; } = string.Empty;

    [Column("sort_order")]
    public int SortOrder { get; set; }
}
