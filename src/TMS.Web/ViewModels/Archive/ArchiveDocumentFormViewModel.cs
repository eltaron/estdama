using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using TMS.Core.Enums;

namespace TMS.Web.ViewModels.Archive;

public class ArchiveDocumentFormViewModel
{
    [Required(ErrorMessageResourceName = "ArchiveForm_DocTypeRequired", ErrorMessageResourceType = typeof(Shared))]
    [Display(Name = "ArchiveForm.DocType")]
    public ArchiveDocumentType DocType { get; set; } = ArchiveDocumentType.Incoming;

    [Required(ErrorMessageResourceName = "ArchiveForm_DocumentDateRequired", ErrorMessageResourceType = typeof(Shared))]
    [Display(Name = "ArchiveForm.DocumentDate")]
    public DateTime DocumentDate { get; set; } = DateTime.Today;

    [Required(ErrorMessageResourceName = "ArchiveForm_TitleArRequired", ErrorMessageResourceType = typeof(Shared))]
    [StringLength(300)]
    [Display(Name = "ArchiveForm.TitleAr")]
    public string TitleAr { get; set; } = string.Empty;

    [StringLength(1000)]
    [Display(Name = "ArchiveForm.Subject")]
    public string? Subject { get; set; }

    [StringLength(200)]
    [Display(Name = "ArchiveForm.SenderName")]
    public string? SenderName { get; set; }

    [StringLength(200)]
    [Display(Name = "ArchiveForm.RecipientName")]
    public string? RecipientName { get; set; }

    [Display(Name = "ArchiveForm.DepartmentId")]
    public int? DepartmentId { get; set; }

    [StringLength(2000)]
    [Display(Name = "ArchiveForm.Notes")]
    public string? Notes { get; set; }

    [Required(ErrorMessageResourceName = "ArchiveForm_FileRequired", ErrorMessageResourceType = typeof(Shared))]
    [Display(Name = "ArchiveForm.File")]
    public IFormFile? File { get; set; }
}