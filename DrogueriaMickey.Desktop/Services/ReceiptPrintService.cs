using System.Drawing.Printing;
using GdiFont = System.Drawing.Font;
using GdiFontStyle = System.Drawing.FontStyle;
using GdiBrushes = System.Drawing.Brushes;
using GdiRectangleF = System.Drawing.RectangleF;
using GdiStringFormat = System.Drawing.StringFormat;
using GdiStringAlignment = System.Drawing.StringAlignment;
using GdiPens = System.Drawing.Pens;
using System.Windows;
using System.Windows.Media;

namespace DrogueriaMickey.Desktop.Services
{

    public class ReporteZPrintService
    {
        private readonly GdiFont _boldFont = new GdiFont("Consolas", 10, GdiFontStyle.Bold);
        private readonly GdiFont _normalFont = new GdiFont("Consolas", 9, GdiFontStyle.Regular);
        private readonly GdiFont _smallFont = new GdiFont("Consolas", 8, GdiFontStyle.Regular);

        private ReporteZDto? _report;

        public void Print(ReporteZDto report, string? printerName = null)
        {
            _report = report;

            var doc = new PrintDocument();
            if (!string.IsNullOrWhiteSpace(printerName))
                doc.PrinterSettings.PrinterName = printerName;

            doc.PrintPage += OnPrintPage;
            doc.Print();
        }

        private void OnPrintPage(object? sender, PrintPageEventArgs e)
        {
            if (_report == null || e.Graphics == null) return;

            var g = e.Graphics;
            float y = 5;
            float width = e.PageBounds.Width > 0 ? e.PageBounds.Width : 280;
            var centerFormat = new GdiStringFormat { Alignment = GdiStringAlignment.Center };
            var rightFormat = new GdiStringFormat { Alignment = GdiStringAlignment.Far };

            void DrawCenter(string text, GdiFont font)
            {
                g.DrawString(text, font, GdiBrushes.Black, new GdiRectangleF(0, y, width, 20), centerFormat);
                y += font.GetHeight(g) + 3;
            }

            void DrawLine(string left, string right, GdiFont font)
            {
                g.DrawString(left, font, GdiBrushes.Black, new GdiRectangleF(0, y, width * 0.6f, 26));
                g.DrawString(right, font, GdiBrushes.Black, new GdiRectangleF(width * 0.6f, y, width * 0.4f, 26), rightFormat);
                y += font.GetHeight(g) + 4;
            }

            void DrawThreeCol(string label, string count, string amount, GdiFont font)
            {
                g.DrawString(label, font, GdiBrushes.Black, new GdiRectangleF(0, y, width * 0.45f, 26));
                g.DrawString(count, font, GdiBrushes.Black, new GdiRectangleF(width * 0.45f, y, width * 0.2f, 26), centerFormat);
                g.DrawString(amount, font, GdiBrushes.Black, new GdiRectangleF(width * 0.6f, y, width * 0.4f, 26), rightFormat);
                y += font.GetHeight(g) + 3;
            }

            // Fila de dos renglones: LABEL [conteo] arriba, $monto abajo —
            // igual al formato real de BRUTO/NETO/EFEC/CAJA No.
            void DrawTwoLineRow(string label, string count, string amount, GdiFont font)
            {
                DrawLine(label, count, font);
                DrawLine("", amount, font);
            }

            void DrawSeparator()
            {
                g.DrawLine(GdiPens.Black, 0, y, width, y);
                y += 6;
            }

            // Total gravado "grueso" (base + IVA) — es lo que muestra el
            // renglón superior GRAVADO 19%, distinto de la BASE IMPONIBLE
            // (que va sin IVA) que aparece más abajo.
            decimal grossGravado = _report.TotalGravados19 + _report.TotalIva19;

            DrawCenter("DROGUERÍA MICKEY", _boldFont);
            DrawCenter(_report.OwnerName, _smallFont);
            DrawCenter($"NIT: {_report.Nit}", _smallFont);
            DrawCenter(_report.Address, _smallFont);
            y += 6;
            DrawCenter($"Z {_report.DateTime}", _normalFont);
            DrawCenter($"{_report.RegistradoraNo}   {_report.RegistroFinal:D6}", _smallFont);
            y += 4;
            DrawCenter("Z DIARIO", _boldFont);
            y += 6;
            DrawSeparator();

            DrawLine("Z DEPTOS", $"{_report.ZNumero:D4}", _smallFont);
            DrawLine("", $"{_report.RegistroInicial:D7}", _smallFont);
            y += 8;

            // EXENTO / GRAVADO: totales completos (incluyendo IVA en el caso
            // de gravado), tal como los reporta la registradora física.
            DrawThreeCol("EXENTO", $"{_report.ExemptCount}", $"${_report.TotalExentos:N0}", _normalFont);
            DrawThreeCol("GRAVADO 19%", $"{_report.TaxedCount19}", $"${grossGravado:N0}", _normalFont);
            y += 4;
            DrawSeparator();

            DrawLine("TL", $"{_report.NumeroVentas}", _smallFont);
            y += 6;
            DrawSeparator();

            DrawLine("Z TOT. FIJOS", $"{_report.ZNumero:D4}", _smallFont);
            DrawLine("", $"{_report.RegistroInicial:D7}", _smallFont);
            y += 8;

            // BRUTO / NETO / EFEC: los tres muestran el mismo número de
            // ventas y el mismo total — EFEC representa TODO el dinero de
            // la jornada (efectivo + Nequi), tal como lo totaliza la
            // registradora física, sin distinguir método de pago.
            DrawTwoLineRow("BRUTO", $"{_report.NumeroVentas}", $"${_report.TotalVendido:N0}", _normalFont);
            DrawTwoLineRow("NETO      No", $"{_report.NumeroVentas}", $"${_report.TotalVendido:N0}", _normalFont);
            DrawLine("EFEC", $"${_report.TotalVendido:N0}", _normalFont);
            y += 4;
            DrawSeparator();

            DrawLine("CLIENTES CT", $"{_report.NumeroVentas}", _smallFont);
            y += 6;
            DrawSeparator();

            // Desglose fiscal: base imponible (sin IVA) + IVA por separado,
            // para el 19% (único tramo real del negocio) y un 5% en cero
            // (categoría que la registradora siempre imprime aunque esté vacía).
            DrawLine("BASE IMPONIB", $"${_report.TotalGravados19:N0}", _smallFont);
            DrawLine("IVA 19%", $"${_report.TotalIva19:N0}", _smallFont);
            DrawLine("BASE IMPONIB", "$0", _smallFont);
            DrawLine("IVA 5%", "$0", _smallFont);
            DrawLine("EXENTOS", $"${_report.TotalExentos:N0}", _smallFont);
            y += 4;

            // Rango de registro en una sola línea, sin partirse — se usa
            // DrawCenter (ancho completo) en vez de DrawLine para evitar
            // que el guión largo "--->" se corte en dos renglones.
            DrawCenter($"{_report.RegistroInicial:D7}--->{_report.RegistroFinal:D7}", _smallFont);
            y += 6;
            DrawSeparator();

            DrawLine("Z FUNC LIBRES", $"{_report.ZNumero:D4}", _smallFont);
            DrawLine("", $"{_report.RegistroInicial:D7}", _smallFont);
            y += 8;

            // CAJA No: número de ventas del día y el total vendido, en el
            // mismo formato de dos renglones.
            DrawTwoLineRow("CAJA No", $"{_report.NumeroVentas}", $"${_report.TotalVendido:N0}", _boldFont);
            y += 8;
            DrawSeparator();
            y += 6;

            DrawCenter("SERVIMOS NO COMPETIMOS", _normalFont);
            DrawCenter("GRACIAS POR SU COMPRA", _normalFont);
        }
    }

    public class DailyReportPrintService
    {
        private readonly GdiFont _boldFont = new GdiFont("Consolas", 10, GdiFontStyle.Bold);
        private readonly GdiFont _normalFont = new GdiFont("Consolas", 9, GdiFontStyle.Regular);

        private DailySalesReportDto? _report;

        public void Print(DailySalesReportDto report, string? printerName = null)
        {
            _report = report;
            var doc = new PrintDocument();
            if (!string.IsNullOrWhiteSpace(printerName))
                doc.PrinterSettings.PrinterName = printerName;
            doc.PrintPage += OnPrintPage;
            doc.Print();
        }

        private void OnPrintPage(object? sender, PrintPageEventArgs e)
        {
            if (_report == null || e.Graphics == null) return;

            var g = e.Graphics;
            float y = 5;
            float width = e.PageBounds.Width > 0 ? e.PageBounds.Width : 280;
            var centerFormat = new GdiStringFormat { Alignment = GdiStringAlignment.Center };
            var rightFormat = new GdiStringFormat { Alignment = GdiStringAlignment.Far };

            void DrawCenter(string text, GdiFont font)
            {
                g.DrawString(text, font, GdiBrushes.Black, new GdiRectangleF(0, y, width, 20), centerFormat);
                y += font.GetHeight(g) + 4;
            }

            var noWrapFormat = new GdiStringFormat { FormatFlags = System.Drawing.StringFormatFlags.NoWrap };

            void DrawLine(string left, string right, GdiFont font)
            {
                g.DrawString(left, font, GdiBrushes.Black, new GdiRectangleF(0, y, width * 0.65f, 26), noWrapFormat);
                g.DrawString(right, font, GdiBrushes.Black, new GdiRectangleF(width * 0.6f, y, width * 0.4f, 26), rightFormat);
                y += font.GetHeight(g) + 6;
            }

            DrawCenter("DROGUERÍA MICKEY", _boldFont);
            DrawCenter("REPORTE DIARIO DE VENTAS", _normalFont);
            DrawCenter(_report.Date, _normalFont);
            y += 8;

            DrawLine("N° de ventas:", $"{_report.SalesCount}", _normalFont);
            DrawLine("Total vendido:", $"${_report.TotalSold:N0}", _boldFont);
            y += 12;
            DrawLine("Total efectivo:", $"${_report.TotalEfectivo:N0}", _normalFont);
            DrawLine("Total Nequi:", $"${_report.TotalNequi:N0}", _normalFont);
            DrawLine("Ticket promedio:", $"${_report.AverageTicket:N0}", _normalFont);
        }
    }
    public class ReceiptPrintService
        {
            private ReceiptDto? _receipt;
            private readonly GdiFont _boldFont = new GdiFont("Consolas", 10, GdiFontStyle.Bold);
            private readonly GdiFont _normalFont = new GdiFont("Consolas", 9, GdiFontStyle.Regular);
            private readonly GdiFont _smallFont = new GdiFont("Consolas", 8, GdiFontStyle.Regular);

            public void Print(ReceiptDto receipt, string? printerName = null)
            {
                _receipt = receipt;

                var doc = new PrintDocument();
                if (!string.IsNullOrWhiteSpace(printerName))
                    doc.PrinterSettings.PrinterName = printerName;

                doc.PrintPage += OnPrintPage;
                doc.Print();
            }

            private void OnPrintPage(object? sender, PrintPageEventArgs e)
            {
                if (_receipt == null || e.Graphics == null) return;

                var g = e.Graphics;
                float y = 5;
                float width = e.PageBounds.Width > 0 ? e.PageBounds.Width : 280;
                var centerFormat = new GdiStringFormat { Alignment = GdiStringAlignment.Center };
                var rightFormat = new GdiStringFormat { Alignment = GdiStringAlignment.Far };

                void DrawCenter(string text, GdiFont font)
                {
                    g.DrawString(text, font, GdiBrushes.Black, new GdiRectangleF(0, y, width, 20), centerFormat);
                    y += font.GetHeight(g) + 2;
                }

                void DrawLine(string left, string right, GdiFont font)
                {
                    g.DrawString(left, font, GdiBrushes.Black, new GdiRectangleF(0, y, width * 0.6f, 26));
                    g.DrawString(right, font, GdiBrushes.Black, new GdiRectangleF(width * 0.6f, y, width * 0.4f, 26), rightFormat);
                    y += font.GetHeight(g) + 3;
                }

                void DrawSeparator()
                {
                    g.DrawLine(GdiPens.Black, 0, y, width, y);
                    y += 4;
                }

                DrawCenter("DROGUERÍA MICKEY", _boldFont);
                DrawCenter("FACTURA DE VENTA", _normalFont);
                DrawCenter("CRA 4#3-18 ESQUINA P.PRI", _smallFont);
                DrawCenter("SOLITA CAQUETÁ", _smallFont);
                y += 4;
                DrawLine($"Fecha: {_receipt.Date}", $"Hora: {_receipt.Time}", _smallFont);
                DrawLine($"Venta #{_receipt.SaleNumber}", $"Cajero: {_receipt.CashierName}", _smallFont);
                y += 8;
                DrawSeparator();
                y += 2;

            foreach (var item in _receipt.Items)
            {
                y += 6;

                DrawLine($"{item.Quantity} {item.SaleUnit} - {item.MedicineName}", $"${item.Subtotal:N0}", _normalFont);
                y += 12; // más espacio entre el nombre del producto y la línea de Base/IVA

                if (item.HasIva)
                {
                    g.DrawString($"  Base: ${item.BasePrice:N0}   IVA: ${item.TaxAmount:N0}",
                        _smallFont, GdiBrushes.DimGray, new GdiRectangleF(0, y, width, 20));
                    y += _smallFont.GetHeight(g) + 10;
                }

                y += 12; // más espacio antes de pasar al siguiente producto / separador
            }

            DrawSeparator();

                if (_receipt.TotalTax > 0)
                    DrawLine("IVA incluido:", $"${_receipt.TotalTax:N0}", _smallFont);

                DrawLine("TOTAL:", $"${_receipt.TotalAmount:N0}", _boldFont);
                y += 2;

                var methodLabel = _receipt.PaymentMethod == "nequi" ? "Nequi" : "Efectivo";
                DrawLine("Método de pago:", methodLabel, _smallFont);

                if (_receipt.AmountReceived.HasValue)
                    DrawLine("Recibido:", $"${_receipt.AmountReceived:N0}", _smallFont);
                if (_receipt.ChangeGiven.HasValue)
                    DrawLine("Cambio:", $"${_receipt.ChangeGiven:N0}", _smallFont);

                y += 8;
                DrawSeparator();
                y += 6;

                DrawCenter("- Servimos no Competimos -", _normalFont);
                DrawCenter("GRACIAS POR SU COMPRA", _normalFont);
            }
        }
    }