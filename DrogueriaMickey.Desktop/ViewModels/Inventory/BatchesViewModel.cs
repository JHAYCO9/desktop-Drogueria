using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DrogueriaMickey.Desktop.Services;

namespace DrogueriaMickey.Desktop.ViewModels.Inventory
{
    public partial class BatchesViewModel : ObservableObject
    {
        private readonly MedicineService _medicineService;
        private string _medicineId = string.Empty;

        [ObservableProperty] private string medicineName = string.Empty;
        [ObservableProperty] private ObservableCollection<BatchDto> batches = new();
        [ObservableProperty] private string errorMessage = string.Empty;
        [ObservableProperty] private string successMessage = string.Empty;
        [ObservableProperty] private bool isBusy;

        // Campos del formulario "Registrar lote" (RF-015) — sin BatchCode,
        // el backend lo genera automático.
        [ObservableProperty] private string newBatchQuantity = string.Empty;
        [ObservableProperty] private string newBatchReceivedAt = System.DateTime.Today.ToString("yyyy-MM-dd");
        [ObservableProperty] private string newBatchExpirationDate = string.Empty;

        // Campos del formulario "Registrar ingreso" (RF-016)
        [ObservableProperty] private BatchDto? selectedBatchForIngreso;
        [ObservableProperty] private string ingresoQuantity = string.Empty;

        // Campos del formulario "Registrar ajuste" (RF-017)
        [ObservableProperty] private BatchDto? selectedBatchForAdjustment;
        [ObservableProperty] private string adjustmentNewQuantity = string.Empty;
        [ObservableProperty] private string adjustmentReason = string.Empty;

        [ObservableProperty] private ObservableCollection<string> ingresoUnits = new() { "pastilla", "sello", "caja" };
        [ObservableProperty] private string selectedIngresoUnit = "pastilla";

        public BatchesViewModel(MedicineService medicineService)
        {
            _medicineService = medicineService;
        }

        public void Initialize(string medicineId, string medicineName)
        {
            _medicineId = medicineId;
            MedicineName = medicineName;
            _ = LoadBatches();
        }

        [RelayCommand]
        private async Task RegisterAdjustment()
        {
            ErrorMessage = string.Empty;
            SuccessMessage = string.Empty;

            if (SelectedBatchForAdjustment == null)
            {
                ErrorMessage = "Selecciona un lote para registrar el ajuste";
                return;
            }

            if (!int.TryParse(AdjustmentNewQuantity, out var newQty) || newQty < 0)
            {
                ErrorMessage = "La cantidad nueva debe ser un número entero mayor o igual a cero";
                return;
            }

            if (string.IsNullOrWhiteSpace(AdjustmentReason))
            {
                ErrorMessage = "El motivo del ajuste es obligatorio";
                return;
            }

            IsBusy = true;
            var result = await _medicineService.RegisterAdjustmentAsync(_medicineId, SelectedBatchForAdjustment.Id, newQty, AdjustmentReason);
            IsBusy = false;

            if (!result.Success)
            {
                ErrorMessage = result.ErrorMessage ?? "No se pudo registrar el ajuste";
                return;
            }

            SuccessMessage = $"Ajuste registrado: {SelectedBatchForAdjustment.BatchCode} pasó de {SelectedBatchForAdjustment.Quantity} a {newQty} unidades";
            AdjustmentNewQuantity = string.Empty;
            AdjustmentReason = string.Empty;
            SelectedBatchForAdjustment = null;

            await LoadBatches();
        }

        [RelayCommand]
        private async Task LoadBatches()
        {
            IsBusy = true;
            var result = await _medicineService.ListBatchesByMedicineAsync(_medicineId);
            IsBusy = false;

            if (!result.Success)
            {
                ErrorMessage = result.ErrorMessage ?? "No se pudieron cargar los lotes";
                return;
            }

            Batches = new ObservableCollection<BatchDto>(result.Data ?? new());
        }

        [RelayCommand]
        private async Task RegisterBatch()
        {
            ErrorMessage = string.Empty;
            SuccessMessage = string.Empty;

            if (!int.TryParse(NewBatchQuantity, out var qty) || qty < 0)
            {
                ErrorMessage = "La cantidad debe ser un número entero mayor o igual a cero";
                return;
            }

            if (string.IsNullOrWhiteSpace(NewBatchExpirationDate))
            {
                ErrorMessage = "La fecha de vencimiento es obligatoria";
                return;
            }

            IsBusy = true;
            var result = await _medicineService.RegisterBatchAsync(_medicineId, qty, NewBatchReceivedAt, NewBatchExpirationDate);
            IsBusy = false;

            if (!result.Success)
            {
                ErrorMessage = result.ErrorMessage ?? "No se pudo registrar el lote";
                return;
            }

            SuccessMessage = $"Lote registrado: {result.Data?.BatchCode}";
            NewBatchQuantity = string.Empty;
            NewBatchExpirationDate = string.Empty;

            await LoadBatches();
        }

        [RelayCommand]
        private async Task RegisterIngreso()
        {
            ErrorMessage = string.Empty;
            SuccessMessage = string.Empty;

            if (SelectedBatchForIngreso == null)
            {
                ErrorMessage = "Selecciona un lote para registrar el ingreso";
                return;
            }

            if (!int.TryParse(IngresoQuantity, out var qty) || qty <= 0)
            {
                ErrorMessage = "La cantidad a ingresar debe ser mayor a cero";
                return;
            }

            IsBusy = true;
            var result = await _medicineService.RegisterIngresoAsync(SelectedBatchForIngreso.Id, qty, SelectedIngresoUnit);
            IsBusy = false;

            if (!result.Success)
            {
                ErrorMessage = result.ErrorMessage ?? "No se pudo registrar el ingreso";
                return;
            }

            SuccessMessage = "Ingreso registrado correctamente";
            IngresoQuantity = string.Empty;
            SelectedBatchForIngreso = null;

            await LoadBatches();
        }
    }
}