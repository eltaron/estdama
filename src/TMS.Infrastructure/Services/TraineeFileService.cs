using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace TMS.Infrastructure.Services;

public class TraineeFileCourse
{
    public string CourseCode { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public string TrainerName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int Hours { get; set; }
    public string Status { get; set; } = string.Empty;      // registration status
    public string PaymentStatus { get; set; } = string.Empty; // payment status
}

public class TraineeFileExam
{
    public string ExamTitle { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public decimal TotalMarks { get; set; }
    public decimal Score { get; set; }
    public decimal PassingMarks { get; set; }
    public bool IsPassed { get; set; }
    public DateTime SubmittedAt { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class TraineeFileData
{
    public string FullNameAr { get; set; } = string.Empty;
    public string FullNameEn { get; set; } = string.Empty;
    public string NationalId { get; set; } = string.Empty;
    public string TraineeType { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public string Organization { get; set; } = string.Empty;
    public string Directorate { get; set; } = string.Empty;
    public string JobGrade { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public List<TraineeFileCourse> Courses { get; set; } = new();
    public List<TraineeFileExam> Exams { get; set; } = new();
}

public interface ITraineeFileService
{
    byte[] GenerateTraineeFilePdf(TraineeFileData data);
}

public class TraineeFileService : ITraineeFileService
{
    private const string Blue = "#00317E";
    private const string Orange = "#EC8133";

    static TraineeFileService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GenerateTraineeFilePdf(TraineeFileData data)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(t => t.FontFamily("Arial"));

                page.Header().Column(col =>
                {
                    col.Spacing(4);
                    col.Item().Row(row =>
                    {
                        row.ConstantItem(58).Height(38).Element(c =>
                        {
                            // Aspect-fitted placeholder; users may swap for a tracked logo
                        });
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().AlignCenter().Text("الملف التدريبي الكامل للمتدرب")
                                .FontSize(20).Bold().FontColor(Blue);
                            c.Item().AlignCenter().Text("Estidama | نظام إدارة التدريب")
                                .FontSize(11).FontColor(Colors.Grey.Darken1);
                        });
                    });
                    col.Item().LineHorizontal(2).LineColor(Orange);
                });

                page.Content().PaddingVertical(12).Column(col =>
                {
                    col.Spacing(12);

                    // ---- 1. Personal info ----
                    SectionTitle(col, "1) المعلومات الشخصية");
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(120);
                            c.RelativeColumn();
                            c.ConstantColumn(120);
                            c.RelativeColumn();
                        });

                        // label/value helper builds 4 cells: (label1,value1,label2,value2)
                        void Pair(string l1, string v1, string l2, string v2)
                        {
                            LabelCell(table, l1); ValueCell(table, v1);
                            LabelCell(table, l2); ValueCell(table, v2);
                        }

                        Pair("الاسم (عربي)", data.FullNameAr, "الاسم (إنجليزي)", data.FullNameEn);
                        Pair("الرقم القومي", data.NationalId, "البريد الإلكتروني", data.Email);
                        Pair("نوع المتدرب", data.TraineeType, "المسمى الوظيفي", data.JobTitle);
                        Pair("الجهة التابع لها", data.Organization, "الإدارة", data.Directorate);
                        Pair("الدرجة الوظيفية", data.JobGrade, "الهاتف", data.Phone);
                        Pair("العنوان", data.Address, "", "");
                    });

                    // ---- 2. Courses taken ----
                    SectionTitle(col, "2) الدورات التي تم اجتيازها");
                    if (data.Courses.Any())
                    {
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(1.2f);
                                c.RelativeColumn(1.6f);
                                c.RelativeColumn(1.2f);
                                c.RelativeColumn(1.1f);
                                c.RelativeColumn(0.8f);
                                c.RelativeColumn(1f);
                            });
                            HeaderRow(table, "كود الدورة", "اسم الدورة", "المدرب",
                                "البداية - النهاية", "الساعات", "الحالة");
                            foreach (var c in data.Courses)
                            {
                                DetailRow(table, c.CourseCode, c.CourseName, c.TrainerName,
                                    $"{c.StartDate:yyyy-MM-dd} إلى {c.EndDate:yyyy-MM-dd}",
                                    c.Hours.ToString(), c.Status);
                            }
                        });
                    }
                    else
                    {
                        EmptyRow(col, "لم يسجل المتدرب في أي دورة");
                    }

                    // ---- 3. Exams & results ----
                    SectionTitle(col, "3) الاختبارات والنتائج");
                    if (data.Exams.Any())
                    {
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(1.6f);
                                c.RelativeColumn(1.4f);
                                c.RelativeColumn(1f);
                                c.RelativeColumn(1f);
                                c.RelativeColumn(1f);
                                c.RelativeColumn(1f);
                            });
                            HeaderRow(table, "الاختبار", "الدورة", "الدرجة", "مجموع النجاح",
                                "التاريخ", "النتيجة");
                            foreach (var e in data.Exams)
                            {
                                var pass = e.IsPassed ? "ناجح" : "راسب";
                                DetailRow(table, e.ExamTitle, e.CourseName,
                                    $"{e.Score} / {e.TotalMarks}",
                                    e.PassingMarks.ToString(),
                                    e.SubmittedAt.ToString("yyyy-MM-dd"), pass);
                            }
                        });
                    }
                    else
                    {
                        EmptyRow(col, "لم يؤدِّ المتدرب أي اختبار بعد");
                    }
                });

                page.Footer().AlignCenter().Text(
                    $"تم إنشاء الملف في {data.GeneratedAt:yyyy-MM-dd HH:mm} — Estidama")
                    .FontSize(9).FontColor(Colors.Grey.Medium);
            });
        }).GeneratePdf();
    }

    private static void SectionTitle(ColumnDescriptor col, string title)
    {
        col.Item().PaddingBottom(2).Text(title).FontSize(13).Bold().FontColor(Blue);
    }

    private static void LabelCell(TableDescriptor table, string label)
    {
        table.Cell().Border(0.5f).Background("#F2F5FA").Padding(5)
            .Text(label).FontSize(9).Bold();
    }

    private static void ValueCell(TableDescriptor table, string value)
    {
        table.Cell().Border(0.5f).Padding(5)
            .Text(string.IsNullOrEmpty(value) ? "-" : value).FontSize(9);
    }

    private static void HeaderRow(TableDescriptor table, params string[] headers)
    {
        foreach (var h in headers)
        {
            table.Cell().Border(0.5f).Background(Blue).Padding(5)
                .Text(h).FontSize(9).Bold().FontColor(Colors.White);
        }
    }

    private static void DetailRow(TableDescriptor table, params string[] cells)
    {
        foreach (var c in cells)
        {
            table.Cell().Border(0.5f).Padding(5).Text(c).FontSize(9);
        }
    }

    private static void EmptyRow(ColumnDescriptor col, string message)
    {
        col.Item().Padding(8).Border(0.5f).Background("#FBFBFB")
            .Text(message).FontSize(10).FontColor(Colors.Grey.Darken1).AlignCenter();
    }
}