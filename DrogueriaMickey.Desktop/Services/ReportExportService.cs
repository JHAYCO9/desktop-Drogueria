using System.IO;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace DrogueriaMickey.Desktop.Services
{
    // RF-069: exporta cualquier reporte tabular (inventario, próximos a
    // vencer, vencidos, movimientos) o de resumen (diario, mensual) a
    // PDF o Excel, reutilizable desde cualquier pestaña de Reportes.
    public class ReportExportService
    {
        static ReportExportService()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        // ===================== RESUMEN (clave/valor) =====================

        public void ExportSummaryToPdf(string title, List<(string Label, string Value)> rows, string filePath)
        {
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);
                    page.Header().Text(title).FontSize(16).Bold();
                    page.Content().PaddingTop(10).Column(col =>
                    {
                        foreach (var (label, value) in rows)
                        {
                            col.Item().Row(row =>
                            {
                                row.RelativeItem().Text(label);
                                row.RelativeItem().AlignRight().Text(value).Bold();
                            });
                            col.Item().PaddingVertical(4);
                        }
                    });
                    page.Footer().AlignCenter().Text($"Droguería Mickey — generado {DateTime.Now:yyyy-MM-dd HH:mm}").FontSize(8);
                });
            }).GeneratePdf(filePath);
        }

        public void ExportSummaryToExcel(string title, List<(string Label, string Value)> rows, string filePath)
        {
            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add(title.Length > 31 ? title.Substring(0, 31) : title);

            sheet.Cell(1, 1).Value = title;
            sheet.Cell(1, 1).Style.Font.Bold = true;
            sheet.Cell(1, 1).Style.Font.FontSize = 14;

            int r = 3;
            foreach (var (label, value) in rows)
            {
                sheet.Cell(r, 1).Value = label;
                sheet.Cell(r, 2).Value = value;
                r++;
            }

            sheet.Columns().AdjustToContents();
            workbook.SaveAs(filePath);
        }

        // ===================== TABLA (filas/columnas) =====================

        public void ExportTableToPdf(string title, List<string> headers, List<List<string>> rows, string filePath)
        {
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(20);
                    page.Size(PageSizes.A4.Landscape());
                    page.Header().Text(title).FontSize(16).Bold();
                    page.Content().PaddingTop(10).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            foreach (var _ in headers) columns.RelativeColumn();
                        });

                        table.Header(header =>
                        {
                            foreach (var h in headers)
                                header.Cell().Background(Colors.Blue.Lighten4).Padding(4).Text(h).Bold();
                        });

                        foreach (var row in rows)
                        {
                            foreach (var cell in row)
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(cell);
                        }
                    });
                    page.Footer().AlignCenter().Text($"Droguería Mickey — generado {DateTime.Now:yyyy-MM-dd HH:mm}").FontSize(8);
                });
            }).GeneratePdf(filePath);
        }

        public void ExportTableToExcel(string title, List<string> headers, List<List<string>> rows, string filePath)
        {
            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add(title.Length > 31 ? title.Substring(0, 31) : title);

            for (int c = 0; c < headers.Count; c++)
            {
                sheet.Cell(1, c + 1).Value = headers[c];
                sheet.Cell(1, c + 1).Style.Font.Bold = true;
                sheet.Cell(1, c + 1).Style.Fill.BackgroundColor = XLColor.LightBlue;
            }

            for (int r = 0; r < rows.Count; r++)
            {
                for (int c = 0; c < rows[r].Count; c++)
                {
                    sheet.Cell(r + 2, c + 1).Value = rows[r][c];
                }
            }

            sheet.Columns().AdjustToContents();
            workbook.SaveAs(filePath);
        }
    }
}