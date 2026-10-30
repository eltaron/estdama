using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace TMS.Infrastructure.Services;

public class CertificateData
{
    public string SerialNumber { get; set; } = string.Empty;
    public string TraineeNameAr { get; set; } = string.Empty;
    public string CourseNameAr { get; set; } = string.Empty;
    public DateTime IssueDate { get; set; }
    public string DurationHours { get; set; } = string.Empty;
}

public interface ICertificateService
{
    byte[] GenerateCertificatePdf(CertificateData data);
}

public class CertificateService : ICertificateService
{
    static CertificateService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GenerateCertificatePdf(CertificateData data)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(30);

                page.Content().Border(3).BorderColor(Colors.Grey.Lighten1).Padding(20)
                    .Column(col =>
                    {
                        col.Spacing(15);

                        col.Item().AlignCenter().Text("المركز التدريبي")
                            .FontSize(28).Bold().FontColor(Colors.Blue.Darken2);

                        col.Item().AlignCenter().Text("شهادة إتمام دورة تدريبية")
                            .FontSize(22).Bold();

                        col.Item().AlignCenter().Text("تشهد هذه الشهادة بأن")
                            .FontSize(14);

                        col.Item().AlignCenter().Text(data.TraineeNameAr)
                            .FontSize(26).Bold().FontColor(Colors.Blue.Darken1);

                        col.Item().AlignCenter().Text("قد أتم بنجاح دورة")
                            .FontSize(14);

                        col.Item().AlignCenter().Text(data.CourseNameAr)
                            .FontSize(20).Bold();

                        col.Item().AlignCenter().Text($"بإجمالي {data.DurationHours} ساعة تدريبية")
                            .FontSize(13);

                        col.Item().PaddingTop(20).Row(row =>
                        {
                            row.RelativeItem().AlignLeft().Column(c =>
                            {
                                c.Item().Text($"رقم الشهادة: {data.SerialNumber}").FontSize(11);
                                c.Item().Text($"تاريخ الإصدار: {data.IssueDate:yyyy-MM-dd}").FontSize(11);
                            });

                            row.RelativeItem().AlignRight().Text("توقيع المدير")
                                .FontSize(11).Italic();
                        });
                    });
            });
        }).GeneratePdf();
    }
}
