using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DrogueriaMickey.Desktop.Services;
using System.Xml.Linq;

namespace DrogueriaMickey.Desktop.ViewModels.Inventory
{
    public partial class MedicineEditViewModel : ObservableObject
    {
        private readonly MedicineService _medicineService;

        [ObservableProperty] private string alertDays = "30";
        [ObservableProperty] private string barcode = string.Empty;
        [ObservableProperty] private string name = string.Empty;
        [ObservableProperty] private string laboratory = string.Empty;
        [ObservableProperty] private string concentration = string.Empty;
        [ObservableProperty] private string presentation = string.Empty;
        [ObservableProperty] private string pricePerPill = "0";
        [ObservableProperty] private string pricePerSeal = "0";
        [ObservableProperty] private string pricePerBox = "0";
        [ObservableProperty] private string minStock = "0"; // RF-020
        [ObservableProperty] private string errorMessage = string.Empty;
        [ObservableProperty] private bool isBusy;

        public event Action? Saved;

        public MedicineEditViewModel(MedicineService medicineService)
        {
            _medicineService = medicineService;
        }

        // Método dedicado para cuando el código llega desde el lector
        // (RF-026): permite escanear directo en el formulario de registro
        // sin tener que teclearlo manualmente.
        public void SetScannedBarcode(string scannedCode)
        {
            Barcode = scannedCode;
        }

        [RelayCommand]
        private async Task Save()
        {
            ErrorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(Barcode) || string.IsNullOrWhiteSpace(Name))
            {
                ErrorMessage = "Código de barras y nombre son obligatorios";
                return;
            }

            if (!decimal.TryParse(PricePerPill, out var pill) || pill < 0 ||
                !decimal.TryParse(PricePerSeal, out var seal) || seal < 0 ||
                !decimal.TryParse(PricePerBox, out var box) || box < 0)
            {
                ErrorMessage = "Los precios deben ser números válidos mayores o iguales a cero";
                return;
            }

            if (!int.TryParse(MinStock, out var minStockValue) || minStockValue < 0)
            {
                ErrorMessage = "El stock mínimo debe ser un número entero mayor o igual a cero";
                return;
            }

            IsBusy = true;
            var result = await _medicineService.RegisterMedicineAsync(
                Barcode, Name, Laboratory, Concentration, Presentation,
                pill, seal, box, minStockValue);
            IsBusy = false;

            if (!result.Success)
            {
                ErrorMessage = result.ErrorMessage ?? "No se pudo registrar el medicamento";
                return;
            }

            Saved?.Invoke();
        }
    }
}