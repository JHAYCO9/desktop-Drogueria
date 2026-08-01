using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DrogueriaMickey.Desktop.Services;

namespace DrogueriaMickey.Desktop.ViewModels.Returns
{
    public partial class ReturnCreateViewModel : ObservableObject
    {
        private readonly SaleService _saleService;
        private readonly ReturnService _returnService;

        [ObservableProperty] private string saleNumberInput = string.Empty;
        [ObservableProperty] private SaleDto? loadedSale;
        [ObservableProperty] private ObservableCollection<SaleItemDto> saleItems = new();
        [ObservableProperty] private SaleItemDto? selectedItem;
        [ObservableProperty] private string quantityInput = "1";
        [ObservableProperty] private string reasonInput = string.Empty;
        [ObservableProperty] private string errorMessage = string.Empty;
        [ObservableProperty] private string successMessage = string.Empty;
        [ObservableProperty] private bool isBusy;

        public ReturnCreateViewModel(SaleService saleService, ReturnService returnService)
        {
            _saleService = saleService;
            _returnService = returnService;
        }

        // El cajero/admin busca la venta por su ID (podría venir del
        // historial de ventas con "Consultar" -> "Devolver").
        public void InitializeWithSaleId(string saleId)
        {
            _ = LoadSale(saleId);
        }

        private async Task LoadSale(string saleId)
        {
            ErrorMessage = string.Empty;
            IsBusy = true;
            var result = await _saleService.GetSaleAsync(saleId);
            IsBusy = false;

            if (!result.Success || result.Data == null)
            {
                ErrorMessage = result.ErrorMessage ?? "No se pudo cargar la venta";
                return;
            }

            LoadedSale = result.Data;
            SaleItems = new ObservableCollection<SaleItemDto>(result.Data.Items);
        }

        [RelayCommand]
        private async Task RegisterReturn()
        {
            ErrorMessage = string.Empty;
            SuccessMessage = string.Empty;

            if (LoadedSale == null || SelectedItem == null)
            {
                ErrorMessage = "Selecciona el producto a devolver";
                return;
            }

            if (SelectedItem.IsFullyReturned)
            {
                ErrorMessage = "Este producto ya fue devuelto en su totalidad";
                return;
            }

            if (!int.TryParse(QuantityInput, out var qty) || qty <= 0)
            {
                ErrorMessage = "La cantidad debe ser un número entero mayor a cero";
                return;
            }

            var available = SelectedItem.Quantity - SelectedItem.ReturnedQuantity;
            if (qty > available)
            {
                ErrorMessage = $"Solo puedes devolver hasta {available} unidad(es) de este producto (ya se devolvieron {SelectedItem.ReturnedQuantity} de {SelectedItem.Quantity})";
                return;
            }

            if (string.IsNullOrWhiteSpace(ReasonInput))
            {
                ErrorMessage = "El motivo de la devolución es obligatorio";
                return;
            }

            IsBusy = true;
            var result = await _returnService.RegisterReturnAsync(LoadedSale.Id, SelectedItem.Id, qty, ReasonInput);
            IsBusy = false;

            if (!result.Success)
            {
                ErrorMessage = result.ErrorMessage ?? "No se pudo registrar la devolución";
                return;
            }

            SuccessMessage = "Devolución registrada correctamente. El inventario ya fue actualizado.";
            QuantityInput = "1";
            ReasonInput = string.Empty;
            SelectedItem = null;

            // Recarga la venta para reflejar el nuevo estado de devolución
            await LoadSaleAgain();
        }

        private async Task LoadSaleAgain()
        {
            if (LoadedSale == null) return;
            var result = await _saleService.GetSaleAsync(LoadedSale.Id);
            if (result.Success && result.Data != null)
            {
                LoadedSale = result.Data;
                SaleItems = new ObservableCollection<SaleItemDto>(result.Data.Items);
            }
        }
    }
}