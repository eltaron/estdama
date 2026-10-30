using TMS.Core.Entities.Common;
using TMS.Core.Entities.Identity;
using TMS.Core.Enums;

namespace TMS.Core.Entities.Archive;

public class ArchiveDocument : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public int Year { get; set; }
    public int Sequence { get; set; }
    public ArchiveDocumentType DocType { get; set; } = ArchiveDocumentType.Incoming;
    public string TitleAr { get; set; } = string.Empty;
    public string? Subject { get; set; }
    public DateTime DocumentDate { get; set; }
    public string? SenderName { get; set; }
    public string? RecipientName { get; set; }
    public string? Notes { get; set; }
    public int? DepartmentId { get; set; }
    public string? FilePath { get; set; }
    public string? OriginalFileName { get; set; }
    public string? ContentType { get; set; }
    public long FileSize { get; set; }
    public string? CreatedById { get; set; }

    public Department? Department { get; set; }
    public ApplicationUser? CreatedBy { get; set; }
}