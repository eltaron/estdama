using System.Text;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace TMS.Infrastructure.Services;

public interface IExportService
{
    byte[] Excel(string sheetName, IReadOnlyList<string> headers, IReadOnlyList<object?[]> rows);
    byte[] Pdf(string title, IReadOnlyList<string> headers, IReadOnlyList<object?[]> rows);
}

public class ExportService : IExportService
{
    static ExportService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] Excel(string sheetName, IReadOnlyList<string> headers, IReadOnlyList<object?[]> rows)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add(sheetName);

        for (int c = 0; c < headers.Count; c++)
            sheet.Cell(1, c + 1).Value = headers[c];

        var headerRange = sheet.Range(1, 1, 1, headers.Count);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#195495");
        headerRange.Style.Font.FontColor = XLColor.White;

        for (int r = 0; r < rows.Count; r++)
        {
            for (int c = 0; c < headers.Count; c++)
            {
                var val = rows[r][c];
                var cell = sheet.Cell(r + 2, c + 1);
                if (val is DateTime dt)
                    cell.Value = dt;
                else if (val is decimal dcm)
                    cell.Value = dcm;
                else if (val is double dbl)
                    cell.Value = dbl;
                else if (val is int i)
                    cell.Value = i;
                else
                    cell.Value = val?.ToString() ?? string.Empty;
            }
        }

        sheet.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    public byte[] Pdf(string title, IReadOnlyList<string> headers, IReadOnlyList<object?[]> rows)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(20);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Row(row =>
                {
                    row.RelativeItem().Text(title).SemiBold().FontSize(14);
                    row.ConstantItem(70).Text(DateTime.Now.ToString("yyyy-MM-dd")).FontSize(8).AlignRight();
                });

                if (headers.Count == 0)
                {
                    page.Content().Text("").FontColor(Colors.Grey.Medium);
                    return;
                }

                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        for (int i = 0; i < headers.Count; i++)
                            columns.RelativeColumn(1);
                    });

                    table.Header(cell =>
                    {
                        foreach (var h in headers)
                            cell.Cell().Background("#195495").Padding(4).Text(h).FontColor(Colors.White).Bold();
                    });

                    foreach (var row in rows)
                    {
                        for (int c = 0; c < headers.Count; c++)
                        {
                            var value = Normalize(row[c]);
                            table.Cell().PaddingVertical(3).PaddingHorizontal(4).Text(value);
                        }
                    }
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Estidama | ");
                    x.CurrentPageNumber();
                });
            });
        }).GeneratePdf();
    }

    private static string Normalize(object? value)
    {
        if (value == null) return string.Empty;
        if (value is DateTime dt) return dt.ToString("yyyy-MM-dd");
        if (value is bool b) return b ? "Yes" : "No";
        return value.ToString() ?? string.Empty;
    }
}