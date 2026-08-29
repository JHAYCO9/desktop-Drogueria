using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DrogueriaMickey.Desktop.Services;
using System.Collections.ObjectModel;   
using System.DirectoryServices;
using System.Threading;

namespace DrogueriaMickey.Desktop.ViewModels.Sales
{
    public partial class SalePosViewModel : ObservableObject
    {
        private readonly SaleService _saleService;
        private readonly MedicineService _medicineService;
        private readonly IServiceProvider _services;
        private readonly CashDrawerService _drawerService;
        private readonly ReceiptPrintService _printService;
        private CancellationTokenSource? _searchCts;

        [ObservableProperty] private string nameSearchInput = string.Empty;
        [ObservableProperty] private ObservableCollection<MedicineDto> searchResults = new();
        [ObservableProperty] private bool showSearchResults;
        [ObservableProperty] private string? saleId;
        [ObservableProperty] private int saleNumber;
        [ObservableProperty] private ObservableCollection<SaleItemDto> items = new();
        [ObservableProperty] private decimal totalAmount;
        [ObservableProperty] private decimal totalTaxAmount;
        [ObservableProperty] private string barcodeInput = string.Empty;
        [ObservableProperty] private string errorMessage = string.Empty;
        [ObservableProperty] private string successMessage = string.Empty;
        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private bool hasActiveSale;

        // Estado del "producto en preparación" antes de confirmarlo a la venta
        [ObservableProperty] private MedicineDto? scannedMedicine;
        [ObservableProperty] private string pendingQuantity = "1";
        [ObservableProperty] private string pendingUnit = "pastilla";

        public bool IsPillSelected => PendingUnit == "pastilla";
        public bool IsSealSelected => PendingUnit == "sello";
        public bool IsBoxSelected => PendingUnit == "caja";

        [ObservableProperty] private bool showPendingPanel;

        public SalePosViewModel(SaleService saleService, MedicineService medicineService, IServiceProvider services, ReceiptPrintService printService, CashDrawerService drawerService)
        {
            _saleService = saleService;
            _medicineService = medicineService;
            _services = services;
            _printService = printService;
            _drawerService = drawerService;
            _ = StartSale();
        }

        // RF-031 + RF-032: crea una venta nueva (o retoma la que estaba en
        // progreso). Si no hay caja abierta, el backend responde 409 y se
        // lo mostramos tal cual al cajero.
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
            Items = new ObservableCollection<SaleItemDto>(result.Data.Items);
            TotalAmount = result.Data.TotalAmount;
            TotalTaxAmount = result.Data.TotalTaxAmount;
            HasActiveSale = true;
        }

        // RF-033: busca el medicamento por código de barras, SIN agregarlo
        // todavía — solo lo prepara para que el cajero elija unidad y
        // cantidad con los botones +/-.
        [RelayCommand]
        private async Task ScanBarcode()
        {
            ErrorMessage = string.Empty;

            if (SaleId == null)
            {
                ErrorMessage = "Primero debes iniciar una venta";
                return;
            }

            if (string.IsNullOrWhiteSpace(BarcodeInput))
                return;

            IsBusy = true;
            var result = await _medicineService.GetByBarcodeAsync(BarcodeInput.Trim());
            IsBusy = false;

            BarcodeInput = string.Empty;

            if (!result.Success || result.Data == null)
            {
                ErrorMessage = "No se encontró ningún medicamento con ese código";
                return;
            }

            ScannedMedicine = result.Data;
            PendingQuantity = "1";

            if (result.Data.SellsByPill) PendingUnit = "pastilla";
            else if (result.Data.SellsBySeal) PendingUnit = "sello";
            else if (result.Data.SellsByBox) PendingUnit = "caja";

            // Fuerza el refresco visual de los RadioButton, incluso si PendingUnit
            // no cambió de valor respecto al medicamento anterior.
            OnPropertyChanged(nameof(IsPillSelected));
            OnPropertyChanged(nameof(IsSealSelected));
            OnPropertyChanged(nameof(IsBoxSelected));

            ShowPendingPanel = true;
        }

        partial void OnPendingUnitChanged(string value)   // <- AQUÍ, método aparte
        {
            OnPropertyChanged(nameof(IsPillSelected));
            OnPropertyChanged(nameof(IsSealSelected));
            OnPropertyChanged(nameof(IsBoxSelected));
        }


        [RelayCommand]
        private void IncreaseQuantity()
        {
            if (int.TryParse(PendingQuantity, out var qty))
                PendingQuantity = (qty + 1).ToString();
        }

        [RelayCommand]
        private void DecreaseQuantity()
        {
            if (int.TryParse(PendingQuantity, out var qty) && qty > 1)
                PendingQuantity = (qty - 1).ToString();
        }

        [RelayCommand]
        private void SelectUnit(string unit)
        {
            PendingUnit = unit;
        }

        // Confirma el producto preparado y lo agrega a la venta real
        // (backend) — recién aquí se llama a AddItem.
        [RelayCommand]
        private async Task ConfirmAddItem()
        {
            ErrorMessage = string.Empty;

            if (SaleId == null || ScannedMedicine == null) return;

            if (!int.TryParse(PendingQuantity, out var qty) || qty <= 0)
            {
                ErrorMessage = "La cantidad debe ser un número entero mayor a cero";
                return;
            }

            IsBusy = true;
            var result = await _saleService.AddItemByBarcodeAsync(SaleId, ScannedMedicine.Barcode, qty, PendingUnit);
            IsBusy = false;

            if (!result.Success || result.Data == null)
            {
                ErrorMessage = result.ErrorMessage ?? "No se pudo agregar el producto";
                return;
            }

            ApplySaleUpdate(result.Data);
            CancelPending();
        }

        [RelayCommand]
        private void CancelPending()
        {
            ScannedMedicine = null;
            ShowPendingPanel = false;
            PendingQuantity = "1";
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

        // RF-034: modifica la cantidad de un producto ya agregado.
        [RelayCommand]
        private async Task ChangeQuantity(SaleItemDto item)
        {
            if (SaleId == null) return;

            var input = Microsoft.VisualBasic.Interaction.InputBox(
                $"Nueva cantidad para {item.MedicineName}:", "Modificar cantidad", item.Quantity.ToString());

            if (!int.TryParse(input, out var newQty) || newQty <= 0)
                return;

            IsBusy = true;
            var result = await _saleService.UpdateItemQuantityAsync(SaleId, item.Id, newQty);
            IsBusy = false;

            if (!result.Success || result.Data == null)
            {
                ErrorMessage = result.ErrorMessage ?? "No se pudo actualizar la cantidad";
                return;
            }

            ApplySaleUpdate(result.Data);
        }

        [RelayCommand]
        private async Task IncreaseItemQuantity(SaleItemDto item)
        {
            if (SaleId == null) return;

            IsBusy = true;
            var result = await _saleService.UpdateItemQuantityAsync(SaleId, item.Id, item.Quantity + 1);
            IsBusy = false;

            if (!result.Success || result.Data == null)
            {
                ErrorMessage = result.ErrorMessage ?? "No se pudo aumentar la cantidad";
                return;
            }

            ApplySaleUpdate(result.Data);
        }

        [RelayCommand]
        private async Task DecreaseItemQuantity(SaleItemDto item)
        {
            if (SaleId == null) return;

            if (item.Quantity <= 1)
            {
                // Si ya está en 1, disminuir significa quitar el producto del carrito.
                await RemoveItem(item);
                return;
            }

            IsBusy = true;
            var result = await _saleService.UpdateItemQuantityAsync(SaleId, item.Id, item.Quantity - 1);
            IsBusy = false;

            if (!result.Success || result.Data == null)
            {
                ErrorMessage = result.ErrorMessage ?? "No se pudo disminuir la cantidad";
                return;
            }

            ApplySaleUpdate(result.Data);
        }

        // RF-036
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
            await StartSale();
        }

        // RF-039/040/041/043: abre el diálogo de pago.
        [RelayCommand]
        private void OpenPayment()
        {
            ErrorMessage = string.Empty;

            if (SaleId == null || Items.Count == 0)
            {
                ErrorMessage = "Agrega al menos un producto antes de cobrar";
                return;
            }

            var dialogVm = new PaymentDialogViewModel(_saleService, _printService, _drawerService, SaleId, TotalAmount);
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

        partial void OnNameSearchInputChanged(string value)
        {
            _searchCts?.Cancel();

            if (string.IsNullOrWhiteSpace(value) || value.Trim().Length < 2)
            {
                SearchResults.Clear();
                ShowSearchResults = false;
                return;
            }

            _searchCts = new CancellationTokenSource();
            _ = DebouncedSearch(value, _searchCts.Token);
        }

        // Espera 300ms sin más tecleo antes de buscar, para no mandar una
        // petición al backend por cada letra escrita.
        private async Task DebouncedSearch(string query, CancellationToken token)
        {
            try
            {
                await Task.Delay(300, token);
            }
            catch (TaskCanceledException)
            {
                return;
            }

            if (token.IsCancellationRequested) return;

            var result = await _medicineService.SearchMedicinesAsync(name: query);

            if (token.IsCancellationRequested) return;

            if (result.Success && result.Data != null)
            {
                SearchResults = new ObservableCollection<MedicineDto>(result.Data);
                ShowSearchResults = SearchResults.Count > 0;
            }
            else
            {
                SearchResults.Clear();
                ShowSearchResults = false;
            }
        }

        // Al elegir un resultado de la lista, abre el mismo panel de
        // unidad/cantidad que usa el escaneo por código de barras.
        [RelayCommand]
        private void SelectSearchResult(MedicineDto medicine)
        {
            ScannedMedicine = medicine;
            PendingQuantity = "1";

            if (medicine.SellsByPill) PendingUnit = "pastilla";
            else if (medicine.SellsBySeal) PendingUnit = "sello";
            else if (medicine.SellsByBox) PendingUnit = "caja";

            OnPropertyChanged(nameof(IsPillSelected));
            OnPropertyChanged(nameof(IsSealSelected));
            OnPropertyChanged(nameof(IsBoxSelected));

            ShowPendingPanel = true;

            NameSearchInput = string.Empty;
            SearchResults.Clear();
            ShowSearchResults = false;
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