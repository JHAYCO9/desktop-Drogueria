using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DrogueriaMickey.Desktop.Navigation;
using DrogueriaMickey.Desktop.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DrogueriaMickey.Desktop.ViewModels.CashRegister
{
    public partial class CashRegisterViewModel : ObservableObject
    {
        private readonly CashRegisterService _cashService;
        private readonly IServiceProvider _services;
        private readonly NavigationService _navigation;
        private readonly ReceiptPrintService _printService;
        private readonly ReporteZPrintService _reporteZPrintService;
        private readonly CashDrawerService _drawerService;
        private string? _currentCashRegisterId;

        [ObservableProperty] private bool isOpen;
        [ObservableProperty] private decimal montoInicial;
        [ObservableProperty] private string fechaApertura = string.Empty;
        [ObservableProperty] private string abiertoPor = string.Empty;
        [ObservableProperty] private decimal totalVentasEfectivo;
        [ObservableProperty] private decimal totalVentasOtrosMedios;
        [ObservableProperty] private decimal totalIngresosManual;
        [ObservableProperty] private decimal totalEgresosManual;
        [ObservableProperty] private decimal saldoTeorico;
        [ObservableProperty] private ObservableCollection<ManualMovementItemDto> listaMovimientos = new();

        // RF-048: apertura
        [ObservableProperty] private string openingAmountInput = string.Empty;

        // RF-018: movimiento manual
        [ObservableProperty] private string manualMovementType = "ingreso_manual";
        [ObservableProperty] private string manualAmountInput = string.Empty;
        [ObservableProperty] private string manualReasonInput = string.Empty;

        // RF-052/053/054: cierre
        [ObservableProperty] private string countedAmountInput = string.Empty;
        [ObservableProperty] private string differenceReasonInput = string.Empty;
        [ObservableProperty] private bool showCloseConfirmation;
        [ObservableProperty] private decimal expectedAmountPreview;
        [ObservableProperty] private decimal differencePreview;
        [ObservableProperty] private bool hasDifference;

        [ObservableProperty] private string errorMessage = string.Empty;
        [ObservableProperty] private string successMessage = string.Empty;
        [ObservableProperty] private bool isBusy;

        public string StatusLabel => IsOpen ? "Abierta" : "Cerrada";

        public CashRegisterViewModel(
            CashRegisterService cashService, IServiceProvider services, NavigationService navigation,
            ReceiptPrintService printService, ReporteZPrintService reporteZPrintService, CashDrawerService drawerService)
        {
            _cashService = cashService;
            _services = services;
            _navigation = navigation;
            _printService = printService;
            _reporteZPrintService = reporteZPrintService;
            _drawerService = drawerService;
            _ = LoadStatus();
        }

        // RF-019: estado detallado en tiempo real.
        [RelayCommand]
        private async Task LoadStatus()
        {
            ErrorMessage = string.Empty;
            IsBusy = true;
            var result = await _cashService.GetDetailedStatusAsync();
            IsBusy = false;

            if (!result.Success || result.Data == null)
            {
                ErrorMessage = result.ErrorMessage ?? "No se pudo consultar el estado de la caja";
                return;
            }

            var d = result.Data;
            IsOpen = d.IsOpen;
            _currentCashRegisterId = d.SessionId;
            MontoInicial = d.MontoInicial;
            FechaApertura = d.FechaApertura ?? string.Empty;
            AbiertoPor = d.AbiertoPor ?? string.Empty;
            TotalVentasEfectivo = d.TotalVentasEfectivo;
            TotalVentasOtrosMedios = d.TotalVentasOtrosMedios;
            TotalIngresosManual = d.TotalIngresosManual;
            TotalEgresosManual = d.TotalEgresosManual;
            SaldoTeorico = d.SaldoTeorico;
            ListaMovimientos = new ObservableCollection<ManualMovementItemDto>(d.ListaMovimientos);
            ShowCloseConfirmation = false;
            OnPropertyChanged(nameof(StatusLabel));
        }

        // RF-048
        [RelayCommand]
        private async Task OpenCashRegister()
        {
            ErrorMessage = string.Empty;
            SuccessMessage = string.Empty;

            if (!decimal.TryParse(OpeningAmountInput, out var amount) || amount < 0)
            {
                ErrorMessage = "El valor inicial debe ser un número mayor o igual a cero";
                return;
            }

            IsBusy = true;
            var result = await _cashService.OpenAsync(amount);
            IsBusy = false;

            if (!result.Success)
            {
                ErrorMessage = result.ErrorMessage ?? "No se pudo abrir la caja";
                return;
            }

            SuccessMessage = "Caja abierta correctamente";
            OpeningAmountInput = string.Empty;
            await LoadStatus();
        }

        // RF-018: registrar ingreso/egreso manual.
        [RelayCommand]
        private async Task RegisterManualMovement()
        {
            ErrorMessage = string.Empty;
            SuccessMessage = string.Empty;

            if (!decimal.TryParse(ManualAmountInput, out var amount) || amount <= 0)
            {
                ErrorMessage = "El monto debe ser un número mayor a cero";
                return;
            }

            if (string.IsNullOrWhiteSpace(ManualReasonInput) || ManualReasonInput.Length < 3)
            {
                ErrorMessage = "La justificación debe tener al menos 3 caracteres";
                return;
            }

            IsBusy = true;
            var result = await _cashService.RegisterManualMovementAsync(ManualMovementType, amount, ManualReasonInput);
            IsBusy = false;

            if (!result.Success)
            {
                ErrorMessage = result.ErrorMessage ?? "No se pudo registrar el movimiento";
                return;
            }

            SuccessMessage = "Movimiento registrado correctamente";
            ManualAmountInput = string.Empty;
            ManualReasonInput = string.Empty;
            await LoadStatus();
        }

        [RelayCommand]
        private void PreviewClose()
        {
            ErrorMessage = string.Empty;

            if (!decimal.TryParse(CountedAmountInput, out var counted) || counted < 0)
            {
                ErrorMessage = "El valor contado debe ser un número mayor o igual a cero";
                return;
            }

            ExpectedAmountPreview = SaldoTeorico;
            DifferencePreview = counted - SaldoTeorico;
            HasDifference = DifferencePreview != 0;
            ShowCloseConfirmation = true;
        }

        [RelayCommand]
        private async Task ConfirmClose()
        {
            ErrorMessage = string.Empty;

            if (!decimal.TryParse(CountedAmountInput, out var counted))
            {
                ErrorMessage = "Valor contado inválido";
                return;
            }

            var confirm = System.Windows.MessageBox.Show(
                $"¿Confirmas el cierre de caja?\n\nEsperado: ${ExpectedAmountPreview:N0}\nContado: ${counted:N0}\nDiferencia: ${DifferencePreview:N0}",
                "Confirmar cierre de caja", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning);

            if (confirm != System.Windows.MessageBoxResult.Yes) return;

            IsBusy = true;

            var cashRegisterId = _currentCashRegisterId;
            var result = await _cashService.CloseAsync(counted, DifferenceReasonInput);

            if (!result.Success || result.Data == null)
            {
                IsBusy = false;
                ErrorMessage = result.ErrorMessage ?? "No se pudo cerrar la caja";
                return;
            }

            var discrepancyLabel = result.Data.DiscrepancyType switch
            {
                "EXCESS" => "Sobrante",
                "DEFICIT" => "Faltante",
                _ => "Cuadre exacto"
            };
            SuccessMessage = $"Caja cerrada. {discrepancyLabel}: ${result.Data.DifferenceAmount:N0}";
            CountedAmountInput = string.Empty;
            DifferenceReasonInput = string.Empty;
            ShowCloseConfirmation = false;

            if (!string.IsNullOrEmpty(cashRegisterId))
            {
                var reportResult = await _cashService.GetReporteZAsync(cashRegisterId);
                if (reportResult.Success && reportResult.Data != null)
                {
                    try { _reporteZPrintService.Print(reportResult.Data); }
                    catch (Exception ex) { SuccessMessage += $"\n(No se pudo imprimir el Reporte Z: {ex.Message})"; }

                    try { _drawerService.OpenDrawer(); }
                    catch (Exception ex) { SuccessMessage += $"\n(No se pudo abrir la caja: {ex.Message})"; }
                }
            }

            IsBusy = false;
            await LoadStatus();
        }

        [RelayCommand]
        private void CancelClose()
        {
            ShowCloseConfirmation = false;
            CountedAmountInput = string.Empty;
            DifferenceReasonInput = string.Empty;
        }

        [RelayCommand]
        private void ViewMovements()
        {
            var vm = _services.GetRequiredService<CashMovementsViewModel>();
            var view = new Views.CashRegister.CashMovementsView { DataContext = vm };
            _navigation.NavigateTo(view);
        }

        // RF-056
        [RelayCommand]
        private void ViewHistory()
        {
            var vm = _services.GetRequiredService<CashHistoryViewModel>();
            var view = new Views.CashRegister.CashHistoryView { DataContext = vm };
            _navigation.NavigateTo(view);
        }

        [RelayCommand]
        private void GoToSales()
        {
            var vm = _services.GetRequiredService<ViewModels.Sales.SalePosViewModel>();
            var view = new Views.Sales.SalePosView { DataContext = vm };
            _navigation.NavigateTo(view);
        }
    }
}