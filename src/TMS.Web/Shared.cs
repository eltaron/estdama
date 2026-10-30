using System.Resources;

namespace TMS.Web;

public sealed class Shared
{
    private static ResourceManager? _resourceMan;
    private static readonly Lock _lock = new();

    public static ResourceManager ResourceManager
    {
        get
        {
            lock (_lock)
            {
                _resourceMan ??= new ResourceManager("TMS.Web.Resources.Shared", typeof(Shared).Assembly);
                return _resourceMan;
            }
        }
    }

    public static string CourseForm_NameArRequired => ResourceManager.GetString("CourseForm.NameArRequired") ?? string.Empty;
    public static string CourseForm_NameEnRequired => ResourceManager.GetString("CourseForm.NameEnRequired") ?? string.Empty;
    public static string TraineeForm_FullNameArRequired => ResourceManager.GetString("TraineeForm.FullNameArRequired") ?? string.Empty;
    public static string InstructorForm_FirstNameArRequired => ResourceManager.GetString("InstructorForm.FirstNameArRequired") ?? string.Empty;
    public static string InstructorForm_FirstNameEnRequired => ResourceManager.GetString("InstructorForm.FirstNameEnRequired") ?? string.Empty;
    public static string ArchiveForm_DocTypeRequired => ResourceManager.GetString("ArchiveForm.DocTypeRequired") ?? string.Empty;
    public static string ArchiveForm_DocumentDateRequired => ResourceManager.GetString("ArchiveForm.DocumentDateRequired") ?? string.Empty;
    public static string ArchiveForm_TitleArRequired => ResourceManager.GetString("ArchiveForm.TitleArRequired") ?? string.Empty;
    public static string ArchiveForm_FileRequired => ResourceManager.GetString("ArchiveForm.FileRequired") ?? string.Empty;
    public static string CertificateForm_TraineeRequired => ResourceManager.GetString("Certificates.TraineeRequired") ?? string.Empty;
    public static string CertificateForm_ScheduleRequired => ResourceManager.GetString("Certificates.ScheduleRequired") ?? string.Empty;
}