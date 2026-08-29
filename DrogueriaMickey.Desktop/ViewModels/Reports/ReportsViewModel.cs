using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DrogueriaMickey.Desktop.Services;

namespace DrogueriaMickey.Desktop.ViewModels.Reports
{
    public partial class ReportsViewModel : ObservableObject
    {
        private readonly ReportService _reportService;
        private readonly MedicineService _medicineService;
        private readonly DailyReportPrintService _printService;
        private readonly ReportExportService _exportService;

        // RF-061
        [ObservableProperty] private string dailyDate = DateTime.Today.ToString("yyyy-MM-dd");
        [ObservableProperty] private DailySalesReportDto? dailyReport;

        // RF-062
        [ObservableProperty] private string monthlyYear = DateTime.Today.Year.ToString();
        [ObservableProperty] private string monthlyMonth = DateTime.Today.Month.ToString();
        [ObservableProperty] private MonthlySalesReportDto? monthlyReport;

        // RF-063
        [ObservableProperty] private ObservableCollection<TopSellingProductDto> topProductsReport = new();

        // RF-064
        [ObservableProperty] private ObservableCollection<LowRotationProductDto> lowRotationReport = new();

        // RF-065
        [ObservableProperty] private ObservableCollection<InventoryGeneralItemDto> inventoryReport = new();

        // RF-066
        [ObservableProperty] private string expiringDays = "30";
        [ObservableProperty] private ObservableCollection<BatchDto> expiringReport = new();

        // RF-067
        [ObservableProperty] private ObservableCollection<BatchDto> expiredReport = new();

        // RF-068
        [ObservableProperty] private string movementsFrom = DateTime.Today.AddDays(-7).ToString("yyyy-MM-dd");
        [ObservableProperty] private string movementsTo = DateTime.Today.ToString("yyyy-MM-dd");
        [ObservableProperty] private ObservableCollection<InventoryMovementDto> movementsReport = new();

        [ObservableProperty] private string errorMessage = string.Empty;
        [ObservableProperty] private string successMessage = string.Empty;
        [ObservableProperty] private bool isBusy;

        public ReportsViewModel(ReportService reportService, MedicineService medicineService, DailyReportPrintService printService, ReportExportService exportService)
        {
            _reportService = reportService;
            _medicineService = medicineService;
            _printService = printService;
            _exportService = exportService;
        }

        // RF-061
        [RelayCommand]
        private async Task GenerateDailyReport()
        {
            ErrorMessage = string.Empty;
            IsBusy = true;
            var result = await _reportService.GetDailySalesReportAsync(DailyDate);
            IsBusy = false;

            if (!result.Success || result.Data == null)
            {
                ErrorMessage = result.ErrorMessage ?? "No se pudo generar el reporte";
                return;
            }
            DailyReport = result.Data;
        }

        [RelayCommand]
        private void PrintDailyReport()
        {
            if (DailyReport == null) return;
            try { _printService.Print(DailyReport); }
            catch (Exception ex) { ErrorMessage = $"No se pudo imprimir: {ex.Message}"; }
        }

        // RF-062
        [RelayCommand]
        private async Task GenerateMonthlyReport()
        {
            ErrorMessage = string.Empty;

            if (!int.TryParse(MonthlyYear, out var year) || !int.TryParse(MonthlyMonth, out var month))
            {
                ErrorMessage = "Año y mes deben ser números válidos";
                return;
            }

            IsBusy = true;
            var result = await _reportService.GetMonthlySalesReportAsync(year, month);
            IsBusy = false;

            if (!result.Success || result.Data == null)
            {
                ErrorMessage = result.ErrorMessage ?? "No se pudo generar el reporte mensual";
                return;
            }
            MonthlyReport = result.Data;
        }

        [RelayCommand]
        private async Task GenerateTopProductsReport()
        {
            ErrorMessage = string.Empty;
            IsBusy = true;
            var result = await _reportService.GetTopSellingProductsAsync();
            IsBusy = false;

            if (!result.Success || result.Data == null)
            {
                ErrorMessage = result.ErrorMessage ?? "No se pudo generar el reporte";
                return;
            }
            TopProductsReport = new ObservableCollection<TopSellingProductDto>(result.Data.Products);
        }

        [RelayCommand]
        private async Task GenerateLowRotationReport()
        {
            ErrorMessage = string.Empty;
            IsBusy = true;
            var result = await _reportService.GetLowRotationProductsAsync();
            IsBusy = false;

            if (!result.Success || result.Data == null)
            {
                ErrorMessage = result.ErrorMessage ?? "No se pudo generar el reporte";
                return;
            }
            LowRotationReport = new ObservableCollection<LowRotationProductDto>(result.Data.Products);
        }

        // RF-065
        [RelayCommand]
        private async Task GenerateInventoryReport()
        {
            ErrorMessage = string.Empty;
            IsBusy = true;
            var result = await _medicineService.GetGeneralInventoryAsync();
            IsBusy = false;

            if (!result.Success)
            {
                ErrorMessage = result.ErrorMessage ?? "No se pudo generar el reporte de inventario";
                return;
            }
            InventoryReport = new ObservableCollection<InventoryGeneralItemDto>(result.Data ?? new());
        }

        // RF-066
        [RelayCommand]
        private async Task GenerateExpiringReport()
        {
            ErrorMessage = string.Empty;

            if (!int.TryParse(ExpiringDays, out var days) || days <= 0)
            {
                ErrorMessage = "Los días deben ser un número entero mayor a cero";
                return;
            }

            IsBusy = true;
            var result = await _medicineService.ListExpiringSoonAsync(days);
            IsBusy = false;

            if (!result.Success)
            {
                ErrorMessage = result.ErrorMessage ?? "No se pudo generar el reporte";
                return;
            }
            ExpiringReport = new ObservableCollection<BatchDto>(result.Data ?? new());
        }

        // RF-067
        [RelayCommand]
        private async Task GenerateExpiredReport()
        {
            ErrorMessage = string.Empty;
            IsBusy = true;
            var result = await _medicineService.ListExpiredAsync();
            IsBusy = false;

            if (!result.Success)
            {
                ErrorMessage = result.ErrorMessage ?? "No se pudo generar el reporte";
                return;
            }
            ExpiredReport = new ObservableCollection<BatchDto>(result.Data ?? new());
        }

        // RF-068
        [RelayCommand]
        private async Task GenerateMovementsReport()
        {
            ErrorMessage = string.Empty;
            IsBusy = true;
            var result = await _medicineService.GetStockHistoryAsync(MovementsFrom, MovementsTo);
            IsBusy = false;

            if (!result.Success)
            {
                ErrorMessage = result.ErrorMessage ?? "No se pudo generar el reporte de movimientos";
                return;
            }
            MovementsReport = new ObservableCollection<InventoryMovementDto>(result.Data ?? new());
        }

        // ===================== Exportar Reporte Diario =====================
        [RelayCommand]
        private void ExportDailyPdf()
        {
            if (DailyReport == null) { ErrorMessage = "Genera el reporte primero"; return; }

            var path = ShowSaveDialog("PDF (*.pdf)|*.pdf", $"ReporteDiario_{DailyReport.Date}.pdf");
            if (path == null) return;

            var rows = new List<(string, string)>
    {
        ("Fecha", DailyReport.Date),
        ("N° de ventas", DailyReport.SalesCount.ToString()),
        ("Total vendido", $"${DailyReport.TotalSold:N0}"),
        ("Total efectivo", $"${DailyReport.TotalEfectivo:N0}"),
        ("Total Nequi", $"${DailyReport.TotalNequi:N0}"),
        ("Ticket promedio", $"${DailyReport.AverageTicket:N0}"),
    };
            _exportService.ExportSummaryToPdf("Reporte Diario de Ventas", rows, path);
            SuccessMessage = "Reporte exportado correctamente";
        }

        [RelayCommand]
        private void ExportDailyExcel()
        {
            if (DailyReport == null) { ErrorMessage = "Genera el reporte primero"; return; }

            var path = ShowSaveDialog("Excel (*.xlsx)|*.xlsx", $"ReporteDiario_{DailyReport.Date}.xlsx");
            if (path == null) return;

            var rows = new List<(string, string)>
    {
        ("Fecha", DailyReport.Date),
        ("N° de ventas", DailyReport.SalesCount.ToString()),
        ("Total vendido", DailyReport.TotalSold.ToString("N0")),
        ("Total efectivo", DailyReport.TotalEfectivo.ToString("N0")),
        ("Total Nequi", DailyReport.TotalNequi.ToString("N0")),
        ("Ticket promedio", DailyReport.AverageTicket.ToString("N0")),
    };
            _exportService.ExportSummaryToExcel("Reporte Diario de Ventas", rows, path);
            SuccessMessage = "Reporte exportado correctamente";
        }

        // ===================== Exportar Reporte Mensual =====================
        [RelayCommand]
        private void ExportMonthlyPdf()
        {
            if (MonthlyReport == null) { ErrorMessage = "Genera el reporte primero"; return; }

            var path = ShowSaveDialog("PDF (*.pdf)|*.pdf", $"ReporteMensual_{MonthlyReport.Month}.pdf");
            if (path == null) return;

            var rows = new List<(string, string)>
    {
        ("Mes", MonthlyReport.Month),
        ("N° de ventas", MonthlyReport.SalesCount.ToString()),
        ("Total vendido", $"${MonthlyReport.TotalSold:N0}"),
        ("Promedio diario", $"${MonthlyReport.AverageDaily:N0}"),
        ("Mejor día", MonthlyReport.BestDay),
        ("Total ese día", $"${MonthlyReport.BestDayTotal:N0}"),
    };
            _exportService.ExportSummaryToPdf("Reporte Mensual de Ventas", rows, path);
            SuccessMessage = "Reporte exportado correctamente";
        }

        [RelayCommand]
        private void ExportMonthlyExcel()
        {
            if (MonthlyReport == null) { ErrorMessage = "Genera el reporte primero"; return; }

            var path = ShowSaveDialog("Excel (*.xlsx)|*.xlsx", $"ReporteMensual_{MonthlyReport.Month}.xlsx");
            if (path == null) return;

            var rows = new List<(string, string)>
    {
        ("Mes", MonthlyReport.Month),
        ("N° de ventas", MonthlyReport.SalesCount.ToString()),
        ("Total vendido", MonthlyReport.TotalSold.ToString("N0")),
        ("Promedio diario", MonthlyReport.AverageDaily.ToString("N0")),
        ("Mejor día", MonthlyReport.BestDay),
        ("Total ese día", MonthlyReport.BestDayTotal.ToString("N0")),
    };
            _exportService.ExportSummaryToExcel("Reporte Mensual de Ventas", rows, path);
            SuccessMessage = "Reporte exportado correctamente";
        }

        // ===================== Exportar Inventario =====================
        [RelayCommand]
        private void ExportInventoryPdf()
        {
            if (InventoryReport.Count == 0) { ErrorMessage = "Genera el reporte primero"; return; }
            var path = ShowSaveDialog("PDF (*.pdf)|*.pdf", "ReporteInventario.pdf");
            if (path == null) return;

            var headers = new List<string> { "Medicamento", "Stock actual", "Stock mínimo", "Lotes", "Próx. vencim." };
            var rows = InventoryReport.Select(i => new List<string> {
        i.Name, i.TotalStock.ToString(), i.MinStock.ToString(), i.AvailableBatches.ToString(), i.NextExpiration
    }).ToList();

            _exportService.ExportTableToPdf("Reporte de Inventario", headers, rows, path);
            SuccessMessage = "Reporte exportado correctamente";
        }

        [RelayCommand]
        private void ExportInventoryExcel()
        {
            if (InventoryReport.Count == 0) { ErrorMessage = "Genera el reporte primero"; return; }
            var path = ShowSaveDialog("Excel (*.xlsx)|*.xlsx", "ReporteInventario.xlsx");
            if (path == null) return;

            var headers = new List<string> { "Medicamento", "Stock actual", "Stock mínimo", "Lotes", "Próx. vencim." };
            var rows = InventoryReport.Select(i => new List<string> {
        i.Name, i.TotalStock.ToString(), i.MinStock.ToString(), i.AvailableBatches.ToString(), i.NextExpiration
    }).ToList();

            _exportService.ExportTableToExcel("Reporte de Inventario", headers, rows, path);
            SuccessMessage = "Reporte exportado correctamente";
        }

        // ===================== Exportar Próximos a Vencer =====================
        [RelayCommand]
        private void ExportExpiringPdf()
        {
            if (ExpiringReport.Count == 0) { ErrorMessage = "Genera el reporte primero"; return; }
            var path = ShowSaveDialog("PDF (*.pdf)|*.pdf", "ReporteProximosAVencer.pdf");
            if (path == null) return;

            var headers = new List<string> { "Medicamento", "Lote", "Vencimiento", "Días restantes", "Cantidad" };
            var rows = ExpiringReport.Select(b => new List<string> {
        b.MedicineName, b.BatchCode, b.ExpirationDate, b.DaysRemaining.ToString(), b.Quantity.ToString()
    }).ToList();

            _exportService.ExportTableToPdf("Medicamentos Próximos a Vencer", headers, rows, path);
            SuccessMessage = "Reporte exportado correctamente";
        }

        [RelayCommand]
        private void ExportExpiringExcel()
        {
            if (ExpiringReport.Count == 0) { ErrorMessage = "Genera el reporte primero"; return; }
            var path = ShowSaveDialog("Excel (*.xlsx)|*.xlsx", "ReporteProximosAVencer.xlsx");
            if (path == null) return;

            var headers = new List<string> { "Medicamento", "Lote", "Vencimiento", "Días restantes", "Cantidad" };
            var rows = ExpiringReport.Select(b => new List<string> {
        b.MedicineName, b.BatchCode, b.ExpirationDate, b.DaysRemaining.ToString(), b.Quantity.ToString()
    }).ToList();

            _exportService.ExportTableToExcel("Medicamentos Próximos a Vencer", headers, rows, path);
            SuccessMessage = "Reporte exportado correctamente";
        }

        // ===================== Exportar Vencidos =====================
        [RelayCommand]
        private void ExportExpiredPdf()
        {
            if (ExpiredReport.Count == 0) { ErrorMessage = "Genera el reporte primero"; return; }
            var path = ShowSaveDialog("PDF (*.pdf)|*.pdf", "ReporteVencidos.pdf");
            if (path == null) return;

            var headers = new List<string> { "Medicamento", "Lote", "Vencimiento", "Cantidad" };
            var rows = ExpiredReport.Select(b => new List<string> {
        b.MedicineName, b.BatchCode, b.ExpirationDate, b.Quantity.ToString()
    }).ToList();

            _exportService.ExportTableToPdf("Medicamentos Vencidos", headers, rows, path);
            SuccessMessage = "Reporte exportado correctamente";
        }

        [RelayCommand]
        private void ExportExpiredExcel()
        {
            if (ExpiredReport.Count == 0) { ErrorMessage = "Genera el reporte primero"; return; }
            var path = ShowSaveDialog("Excel (*.xlsx)|*.xlsx", "ReporteVencidos.xlsx");
            if (path == null) return;

            var headers = new List<string> { "Medicamento", "Lote", "Vencimiento", "Cantidad" };
            var rows = ExpiredReport.Select(b => new List<string> {
        b.MedicineName, b.BatchCode, b.ExpirationDate, b.Quantity.ToString()
    }).ToList();

            _exportService.ExportTableToExcel("Medicamentos Vencidos", headers, rows, path);
            SuccessMessage = "Reporte exportado correctamente";
        }

        // ===================== Exportar Movimientos =====================
        [RelayCommand]
        private void ExportMovementsPdf()
        {
            if (MovementsReport.Count == 0) { ErrorMessage = "Genera el reporte primero"; return; }
            var path = ShowSaveDialog("PDF (*.pdf)|*.pdf", "ReporteMovimientos.pdf");
            if (path == null) return;

            var headers = new List<string> { "Medicamento", "Lote", "Tipo", "Cantidad", "Usuario", "Fecha" };
            var rows = MovementsReport.Select(m => new List<string> {
        m.MedicineName, m.BatchCode, m.MovementType, m.Quantity.ToString(), m.Username, m.CreatedAt
    }).ToList();

            _exportService.ExportTableToPdf("Movimientos de Inventario", headers, rows, path);
            SuccessMessage = "Reporte exportado correctamente";
        }

        [RelayCommand]
        private void ExportMovementsExcel()
        {
            if (MovementsReport.Count == 0) { ErrorMessage = "Genera el reporte primero"; return; }
            var path = ShowSaveDialog("Excel (*.xlsx)|*.xlsx", "ReporteMovimientos.xlsx");
            if (path == null) return;

            var headers = new List<string> { "Medicamento", "Lote", "Tipo", "Cantidad", "Usuario", "Fecha" };
            var rows = MovementsReport.Select(m => new List<string> {
        m.MedicineName, m.BatchCode, m.MovementType, m.Quantity.ToString(), m.Username, m.CreatedAt
    }).ToList();

            _exportService.ExportTableToExcel("Movimientos de Inventario", headers, rows, path);
            SuccessMessage = "Reporte exportado correctamente";
        }

        // ===================== Helper =====================
        private string? ShowSaveDialog(string filter, string defaultFileName)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = filter,
                FileName = defaultFileName
            };
            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }
    }
}