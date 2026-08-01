using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using DrogueriaMickey.Desktop.Navigation;
using DrogueriaMickey.Desktop.Services;

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
        [ObservableProperty] private decimal openingAmount;
        [ObservableProperty] private decimal currentBalance;
        [ObservableProperty] private string openedAt = string.Empty;
        [ObservableProperty] private string openedByName = string.Empty;
        public string StatusLabel => IsOpen ? "Abierta" : "Cerrada";

        // RF-048: formulario de apertura
        [ObservableProperty] private string openingAmountInput = string.Empty;

        // RF-052/053/054: formulario de cierre
        [ObservableProperty] private string countedAmountInput = string.Empty;
        [ObservableProperty] private string differenceReasonInput = string.Empty;
        [ObservableProperty] private bool showCloseConfirmation;
        [ObservableProperty] private decimal expectedAmountPreview;
        [ObservableProperty] private decimal differencePreview;
        [ObservableProperty] private bool hasDifference;

        [ObservableProperty] private string errorMessage = string.Empty;
        [ObservableProperty] private string successMessage = string.Empty;
        [ObservableProperty] private bool isBusy;

        [ObservableProperty] private decimal expectedCash;
        [ObservableProperty] private decimal totalNequiSales;

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

        public CashRegisterViewModel(CashRegisterService cashService, IServiceProvider services, NavigationService navigation)
        {
            _cashService = cashService;
            _services = services;
            _navigation = navigation;
            _ = LoadStatus();
        }

        // RF-049
        [RelayCommand]
        private async Task LoadStatus()
        {
            ErrorMessage = string.Empty;
            IsBusy = true;
            var result = await _cashService.GetStatusAsync();
            IsBusy = false;

            if (!result.Success || result.Data == null)
            {
                ErrorMessage = result.ErrorMessage ?? "No se pudo consultar el estado de la caja";
                return;
            }

            IsOpen = result.Data.IsOpen;
            _currentCashRegisterId = result.Data.Id;
            OnPropertyChanged(nameof(StatusLabel));
            ExpectedCash = result.Data.ExpectedCash;
            TotalNequiSales = result.Data.TotalNequiSales;
            OpeningAmount = result.Data.OpeningAmount;
            CurrentBalance = result.Data.CurrentBalance;
            OpenedAt = result.Data.OpenedAt ?? string.Empty;
            OpenedByName = result.Data.OpenedByName ?? string.Empty;
            ShowCloseConfirmation = false;
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

        // Muestra el resumen antes de confirmar el cierre (RF-052/053)
        [RelayCommand]
        private void PreviewClose()
        {
            ErrorMessage = string.Empty;

            if (!decimal.TryParse(CountedAmountInput, out var counted) || counted < 0)
            {
                ErrorMessage = "El valor contado debe ser un número mayor o igual a cero";
                return;
            }

            ExpectedAmountPreview = ExpectedCash; // <- ya no CurrentBalance
            DifferencePreview = counted - ExpectedCash;
            HasDifference = DifferencePreview != 0;
            ShowCloseConfirmation = true;
        }

        // RF-052 + RF-053 + RF-054
        [RelayCommand]
        private async Task ConfirmClose()
        {
            ErrorMessage = string.Empty;

            if (!decimal.TryParse(CountedAmountInput, out var counted))
            {
                ErrorMessage = "Valor contado inválido";
                return;
            }

            if (HasDifference && string.IsNullOrWhiteSpace(DifferenceReasonInput))
            {
                ErrorMessage = "Debes indicar el motivo de la diferencia antes de cerrar la caja";
                return;
            }

            var confirm = System.Windows.MessageBox.Show(
                $"¿Confirmas el cierre de caja?\n\nEsperado: ${ExpectedAmountPreview:N0}\nContado: ${counted:N0}\nDiferencia: ${DifferencePreview:N0}",
                "Confirmar cierre de caja", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning);

            if (confirm != System.Windows.MessageBoxResult.Yes) return;

            IsBusy = true;

            var cashRegisterId = _currentCashRegisterId; // ver nota abajo
            var result = await _cashService.CloseAsync(counted, DifferenceReasonInput);

            if (!result.Success || result.Data == null)
            {
                IsBusy = false;
                ErrorMessage = result.ErrorMessage ?? "No se pudo cerrar la caja";
                return;
            }

            SuccessMessage = $"Caja cerrada. Diferencia: ${result.Data.DifferenceAmount:N0}";
            CountedAmountInput = string.Empty;
            DifferenceReasonInput = string.Empty;
            ShowCloseConfirmation = false;

            // Genera e imprime el Reporte Z automáticamente, igual que un
            // comprobante — y abre la caja registradora física, ya que el
            // cierre implica que el cajero va a retirar/organizar el efectivo.
            if (!string.IsNullOrEmpty(cashRegisterId))
            {
                var reportResult = await _cashService.GetReporteZAsync(cashRegisterId);
                if (reportResult.Success && reportResult.Data != null)
                {
                    try
                    {
                        _reporteZPrintService.Print(reportResult.Data);
                    }
                    catch (Exception ex)
                    {
                        SuccessMessage += $"\n(No se pudo imprimir el Reporte Z: {ex.Message})";
                    }

                    try
                    {
                        _drawerService.OpenDrawer();
                    }
                    catch (Exception ex)
                    {
                        SuccessMessage += $"\n(No se pudo abrir la caja: {ex.Message})";
                    }
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
        private void GoToSales()
        {
            var vm = _services.GetRequiredService<ViewModels.Sales.SalePosViewModel>();
            var view = new Views.Sales.SalePosView { DataContext = vm };
            _navigation.NavigateTo(view);
        }

        // RF-051
        [RelayCommand]
        private void ViewMovements()
        {
            var vm = _services.GetRequiredService<CashMovementsViewModel>();
            var view = new Views.CashRegister.CashMovementsView { DataContext = vm };
            _navigation.NavigateTo(view);
        }
    }
}