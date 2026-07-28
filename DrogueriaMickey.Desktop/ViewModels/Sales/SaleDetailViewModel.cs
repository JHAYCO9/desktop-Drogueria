using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DrogueriaMickey.Desktop.Services;

namespace DrogueriaMickey.Desktop.ViewModels.Sales
{
    public partial class SaleDetailViewModel : ObservableObject
    {
        private readonly SaleService _saleService;
        private string _saleId = string.Empty;

        [ObservableProperty] private int saleNumber;
        [ObservableProperty] private string status = string.Empty;
        [ObservableProperty] private ObservableCollection<SaleItemDto> items = new();
        [ObservableProperty] private decimal totalAmount;
        [ObservableProperty] private decimal totalTaxAmount;
        [ObservableProperty] private string paymentMethod = string.Empty;
        [ObservableProperty] private string createdAt = string.Empty;
        [ObservableProperty] private string errorMessage = string.Empty;
        [ObservableProperty] private bool isBusy;

        // RF-046
        [ObservableProperty] private string voidReason = string.Empty;
        [ObservableProperty] private bool canVoid;

        public SaleDetailViewModel(SaleService saleService)
        {
            _saleService = saleService;
        }

        public void Initialize(string saleId)
        {
            _saleId = saleId;
            _ = Load();
        }

        [RelayCommand]
        private async Task Load()
        {
            ErrorMessage = string.Empty;
            IsBusy = true;
            var result = await _saleService.GetSaleDetailAsync(_saleId);
            IsBusy = false;

            if (!result.Success || result.Data == null)
            {
                ErrorMessage = result.ErrorMessage ?? "No se pudo cargar el detalle de la venta";
                return;
            }

            SaleNumber = result.Data.SaleNumber;
            Status = result.Data.Status;
            Items = new ObservableCollection<SaleItemDto>(result.Data.Items);
            TotalAmount = result.Data.TotalAmount;
            PaymentMethod = result.Data.PaymentMethod == "nequi" ? "Nequi" : "Efectivo";
            CreatedAt = result.Data.CreatedAt;
            CanVoid = result.Data.Status == "finalizada";
        }

        // RF-046: anular con motivo obligatorio.
        [RelayCommand]
        private async Task VoidSale()
        {
            ErrorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(VoidReason) || VoidReason.Length < 5)
            {
                ErrorMessage = "El motivo de anulación debe tener al menos 5 caracteres";
                return;
            }

            var confirm = System.Windows.MessageBox.Show(
                $"¿Confirmas anular la venta #{SaleNumber}? Esto restaurará el inventario.",
                "Confirmar anulación", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning);

            if (confirm != System.Windows.MessageBoxResult.Yes) return;

            IsBusy = true;
            var result = await _saleService.VoidSaleAsync(_saleId, VoidReason);
            IsBusy = false;

            if (!result.Success)
            {
                ErrorMessage = result.ErrorMessage ?? "No se pudo anular la venta";
                return;
            }

            VoidReason = string.Empty;
            await Load();
        }
    }
}