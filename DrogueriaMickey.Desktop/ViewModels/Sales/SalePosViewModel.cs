using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DrogueriaMickey.Desktop.Services;

namespace DrogueriaMickey.Desktop.ViewModels.Sales
{
    public partial class SalePosViewModel : ObservableObject
    {
        private readonly SaleService _saleService;
        private readonly IServiceProvider _services;
        private readonly CashDrawerService _drawerService;
        private readonly ReceiptPrintService _printService;

        [ObservableProperty] private string? saleId;
        [ObservableProperty] private int saleNumber;
        [ObservableProperty] private ObservableCollection<SaleItemDto> items = new();
        [ObservableProperty] private decimal totalAmount;
        [ObservableProperty] private string barcodeInput = string.Empty;
        [ObservableProperty] private string quantityInput = "1";
        [ObservableProperty] private decimal totalTaxAmount;
        [ObservableProperty] private string errorMessage = string.Empty;
        [ObservableProperty] private string successMessage = string.Empty;
        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private bool hasActiveSale;

        public SalePosViewModel(SaleService saleService, IServiceProvider services, ReceiptPrintService printService, CashDrawerService drawerService)
        {
            _saleService = saleService;
            _services = services;
            _printService = printService;
            _drawerService = drawerService;
            _ = StartSale();
        }

        // RF-031 + RF-032: crea una venta nueva. Si no hay caja abierta,
        // el backend responde 409 y se lo mostramos tal cual al cajero.
        [RelayCommand]
        private async Task StartSale()
        {
            ErrorMessage = string.Empty;
            SuccessMessage = string.Empty;
            IsBusy = true;

            var result = await _saleService.StartSaleAsync();
            IsBusy = false;

            if (!result.Success || result.Data == null)
            {
                ErrorMessage = result.ErrorMessage ?? "No se pudo iniciar la venta";
                return;
            }

            SaleId = result.Data.Id;
            SaleNumber = result.Data.SaleNumber;
            Items = new ObservableCollection<SaleItemDto>();
            TotalAmount = 0;
            HasActiveSale = true;
        }

        // RF-033: agrega un producto por código de barras (lector físico
        // o tecleado). El lote se asigna automáticamente por FEFO en el
        // backend — aquí solo se muestra el resultado.
        [RelayCommand]
        private async Task ScanAndAdd()
        {
            ErrorMessage = string.Empty;

            if (SaleId == null)
            {
                ErrorMessage = "Primero debes iniciar una venta";
                return;
            }

            if (string.IsNullOrWhiteSpace(BarcodeInput))
                return;

            if (!int.TryParse(QuantityInput, out var qty) || qty <= 0)
            {
                ErrorMessage = "La cantidad debe ser un número entero mayor a cero";
                return;
            }

            IsBusy = true;
            var result = await _saleService.AddItemByBarcodeAsync(SaleId, BarcodeInput.Trim(), qty);
            IsBusy = false;

            if (!result.Success || result.Data == null)
            {
                ErrorMessage = result.ErrorMessage ?? "No se pudo agregar el producto";
                BarcodeInput = string.Empty;
                return;
            }

            ApplySaleUpdate(result.Data);
            BarcodeInput = string.Empty;
            QuantityInput = "1";
        }

        // RF-035
        [RelayCommand]
        private async Task RemoveItem(SaleItemDto item)
        {
            if (SaleId == null) return;

            IsBusy = true;
            var result = await _saleService.RemoveItemAsync(SaleId, item.Id);
            IsBusy = false;

            if (!result.Success || result.Data == null)
            {
                ErrorMessage = result.ErrorMessage ?? "No se pudo eliminar el producto";
                return;
            }

            ApplySaleUpdate(result.Data);
        }

        [RelayCommand]
        private async Task CancelSale()
        {
            if (SaleId == null) return;

            var confirm = System.Windows.MessageBox.Show(
                "¿Seguro que quieres cancelar esta venta? Se perderá todo el detalle agregado.",
                "Confirmar cancelación", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning);

            if (confirm != System.Windows.MessageBoxResult.Yes) return;

            IsBusy = true;
            var result = await _saleService.CancelSaleAsync(SaleId);
            IsBusy = false;

            if (!result.Success)
            {
                ErrorMessage = result.ErrorMessage ?? "No se pudo cancelar la venta";
                return;
            }

            ResetSale();
            SuccessMessage = "Venta cancelada correctamente";
            await StartSale(); // inicia la siguiente venta de una vez, sin pasos extra
        }

        [RelayCommand]
        private void OpenPayment()
        {
            ErrorMessage = string.Empty;

            if (SaleId == null || Items.Count == 0)
            {
                ErrorMessage = "Agrega al menos un producto antes de cobrar";
                return;
            }

            var dialogVm = new ViewModels.Sales.PaymentDialogViewModel(_saleService, _printService, _drawerService, SaleId, TotalAmount);
            var dialog = new Views.Sales.PaymentDialog(dialogVm)
            {
                Owner = System.Windows.Application.Current.MainWindow
            };

            dialog.ShowDialog();

            if (dialogVm.PaymentCompleted)
            {
                SuccessMessage = dialogVm.ResultMessage ?? "Venta finalizada correctamente";
                ResetSale();
                _ = StartSale();
            }
        }

        private void ApplySaleUpdate(SaleDto sale)
        {
            Items = new ObservableCollection<SaleItemDto>(sale.Items);
            TotalAmount = sale.TotalAmount;
            TotalTaxAmount = sale.TotalTaxAmount;
        }

        private void ResetSale()
        {
            SaleId = null;
            SaleNumber = 0;
            Items = new ObservableCollection<SaleItemDto>();
            TotalAmount = 0;
            TotalTaxAmount = 0;
            HasActiveSale = false;
        }
    }
}