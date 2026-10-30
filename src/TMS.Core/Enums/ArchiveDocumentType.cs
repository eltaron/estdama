using System.ComponentModel.DataAnnotations;

namespace TMS.Core.Enums;

public enum ArchiveDocumentType
{
    [Display(Name = "وارد")]
    Incoming = 0,

    [Display(Name = "صادر")]
    Outgoing = 1,

    [Display(Name = "مذكرة داخلية")]
    InternalMemo = 2,

    [Display(Name = "أخرى")]
    Other = 3
}