using TMS.Core.Entities.Common;
using TMS.Core.Enums;

namespace TMS.Core.Entities.Resources;

public class Equipment : BaseEntity
{
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public EquipmentCategory Category { get; set; } = EquipmentCategory.Other;
    public EquipmentCondition Condition { get; set; } = EquipmentCondition.Good;
    public bool IsAvailable { get; set; } = true;
    public string? Notes { get; set; }

    public ICollection<EquipmentLoan> Loans { get; set; } = new List<EquipmentLoan>();
}
