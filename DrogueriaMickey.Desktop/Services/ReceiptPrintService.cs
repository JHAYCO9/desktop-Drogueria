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
            y += 8; // más aire antes del separador, para que el cajero no choque con el primer producto
            DrawSeparator();
            y += 2;

            foreach (var item in _receipt.Items)
            {
                // Espacio ANTES de cada producto, para separarlo bien del anterior
                y += 6;

                DrawLine($"{item.Quantity} {item.SaleUnit} - {item.MedicineName}", $"${item.Subtotal:N0}", _normalFont);
                y += 10; // más aire entre el nombre del medicamento y su línea de IVA

                if (item.HasIva)
                {
                    g.DrawString($"  Base: ${item.BasePrice:N0}   IVA: ${item.TaxAmount:N0}",
                        _smallFont, GdiBrushes.DimGray, new GdiRectangleF(0, y, width, 20));
                    y += _smallFont.GetHeight(g) + 10; // más aire después de la línea de IVA
                }

                y += 8; // separación extra entre un producto y el siguiente, siempre
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
            y += 4;
            DrawCenter("- Servimos no Competimos -", _normalFont);
        }
    }
}