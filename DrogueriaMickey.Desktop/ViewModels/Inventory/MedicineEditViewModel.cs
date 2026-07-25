using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DrogueriaMickey.Desktop.Services;
using System.Xml.Linq;

namespace DrogueriaMickey.Desktop.ViewModels.Inventory
{
    public partial class MedicineEditViewModel : ObservableObject
    {
        private readonly MedicineService _medicineService;
        private string? _editingId;

        [ObservableProperty] private string barcode = string.Empty;
        [ObservableProperty] private string name = string.Empty;
        [ObservableProperty] private string laboratory = string.Empty;
        [ObservableProperty] private string concentration = string.Empty;
        [ObservableProperty] private string presentation = string.Empty;
        [ObservableProperty] private string pricePerPill = "0";
        [ObservableProperty] private string pricePerSeal = "0";
        [ObservableProperty] private string pricePerBox = "0";
        [ObservableProperty] private string minStock = "0";
        [ObservableProperty] private bool hasIva;
        [ObservableProperty] private string errorMessage = string.Empty;
        [ObservableProperty] private bool isBusy;

        public bool IsEditMode => _editingId != null;
        public bool IsCreateMode => !IsEditMode;
        public string Title => IsEditMode ? "Editar medicamento" : "Nuevo medicamento";

        public event Action? Saved;

        public MedicineEditViewModel(MedicineService medicineService)
        {
            _medicineService = medicineService;
        }

        public void InitializeCreate() => _editingId = null;

        public void InitializeEdit(MedicineDto existing)
        {
            _editingId = existing.Id;
            Barcode = existing.Barcode; // solo lectura en modo edición, ver XAML
            Name = existing.Name;
            Laboratory = existing.Laboratory;
            Concentration = existing.Concentration;
            Presentation = existing.Presentation;
            PricePerPill = existing.PricePerPill.ToString();
            PricePerSeal = existing.PricePerSeal.ToString();
            PricePerBox = existing.PricePerBox.ToString();
            MinStock = existing.MinStock.ToString();
            HasIva = existing.HasIva;
            OnPropertyChanged(nameof(IsEditMode));
            OnPropertyChanged(nameof(IsCreateMode));
            OnPropertyChanged(nameof(Title));
        }

        public void SetScannedBarcode(string scannedCode) => Barcode = scannedCode;

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
            var result = IsCreateMode
                ? await _medicineService.RegisterMedicineAsync(Barcode, Name, Laboratory, Concentration, Presentation, pill, seal, box, minStockValue, HasIva)
                : await _medicineService.UpdateMedicineAsync(_editingId!, Name, Presentation, Concentration, pill, seal, box, minStockValue, HasIva);

            IsBusy = false;

            if (!result.Success)
            {
                ErrorMessage = result.ErrorMessage ?? "No se pudo guardar el medicamento";
                return;
            }

            Saved?.Invoke();
        }
    }
}