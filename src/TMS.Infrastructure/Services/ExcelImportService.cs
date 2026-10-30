using ClosedXML.Excel;
using TMS.Core.Entities.Identity;

namespace TMS.Infrastructure.Services;

public class EmployeeImportResult
{
    public int TotalRows { get; set; }
    public int Imported { get; set; }
    public int Skipped { get; set; }
    public List<string> Errors { get; set; } = new();
}

public class EmployeeImportDraft
{
    public string FullNameAr { get; set; } = string.Empty;
    public string FullNameEn { get; set; } = string.Empty;
    public string EmployeeNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? JobTitle { get; set; }
    public string? DepartmentName { get; set; }
}

public interface IExcelImportService
{
    byte[] GenerateEmployeeTemplate();
    List<EmployeeImportDraft> ParseEmployeeImport(Stream fileStream);
    Task<EmployeeImportResult> ImportEmployees(Stream fileStream, Func<EmployeeImportDraft, Task<ApplicationUser?>> createUser);
}

public class ExcelImportService : IExcelImportService
{
    public byte[] GenerateEmployeeTemplate()
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Employees");
        ws.Cell(1, 1).Value = "الاسم بالعربي";
        ws.Cell(1, 2).Value = "الاسم بالإنجليزي";
        ws.Cell(1, 3).Value = "الرقم الوظيفي";
        ws.Cell(1, 4).Value = "البريد الإلكتروني";
        ws.Cell(1, 5).Value = "المسمى الوظيفي";
        ws.Cell(1, 6).Value = "القسم";
        ws.Range(1, 1, 1, 6).Style.Font.Bold = true;

        ws.Range(1, 1, 1, 6).Style.Fill.BackgroundColor = XLColor.LightBlue;

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    public List<EmployeeImportDraft> ParseEmployeeImport(Stream fileStream)
    {
        var result = new List<EmployeeImportDraft>();
        using var wb = new XLWorkbook(fileStream);
        var ws = wb.Worksheet(1);
        var rows = ws.RowsUsed();

        for (int row = 2; row <= rows.Count(); row++)
        {
            var nameAr = ws.Cell(row, 1).GetString().Trim();
            var nameEn = ws.Cell(row, 2).GetString().Trim();
            var empNo = ws.Cell(row, 3).GetString().Trim();
            var email = ws.Cell(row, 4).GetString().Trim();

            if (string.IsNullOrEmpty(nameAr) && string.IsNullOrEmpty(empNo))
                continue;

            result.Add(new EmployeeImportDraft
            {
                FullNameAr = nameAr,
                FullNameEn = nameEn,
                EmployeeNumber = empNo,
                Email = email,
                JobTitle = ws.Cell(row, 5).GetString().Trim(),
                DepartmentName = ws.Cell(row, 6).GetString().Trim()
            });
        }

        return result;
    }

    public async Task<EmployeeImportResult> ImportEmployees(Stream fileStream, Func<EmployeeImportDraft, Task<ApplicationUser?>> createUser)
    {
        var drafts = ParseEmployeeImport(fileStream);
        var result = new EmployeeImportResult { TotalRows = drafts.Count };

        foreach (var draft in drafts)
        {
            var user = await createUser(draft);
            if (user != null)
                result.Imported++;
            else
            {
                result.Skipped++;
                result.Errors.Add($"رقم الموظف {draft.EmployeeNumber}: موجود مسبقاً أو بيانات غير صالحة");
            }
        }

        return result;
    }
}
