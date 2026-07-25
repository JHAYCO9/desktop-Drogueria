using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DrogueriaMickey.Desktop.Services;

namespace DrogueriaMickey.Desktop.ViewModels.Inventory
{
    public partial class InventoryMovementsViewModel : ObservableObject
    {
        private readonly MedicineService _medicineService;
        private bool _isHistoryMode; // true = RF-030 (historial con filtro de fechas), false = RF-019 (todos)

        [ObservableProperty] private ObservableCollection<InventoryMovementDto> movements = new();
        [ObservableProperty] private string fromDate = string.Empty;
        [ObservableProperty] private string toDate = string.Empty;
        [ObservableProperty] private string title = "Movimientos de inventario";
        [ObservableProperty] private bool showDateFilters;
        [ObservableProperty] private string errorMessage = string.Empty;
        [ObservableProperty] private bool isBusy;

        public InventoryMovementsViewModel(MedicineService medicineService)
        {
            _medicineService = medicineService;
        }

        // RF-019: todos los movimientos, sin filtro de fecha.
        public void InitializeMovements()
        {
            _isHistoryMode = false;
            Title = "Movimientos de inventario";
            ShowDateFilters = false;
            _ = Load();
        }

        // RF-030: historial con filtro opcional de fechas.
        public void InitializeHistory()
        {
            _isHistoryMode = true;
            Title = "Historial de existencias";
            ShowDateFilters = true;
            _ = Load();
        }

        [RelayCommand]
        private async Task Load()
        {
            IsBusy = true;
            var result = _isHistoryMode
                ? await _medicineService.GetStockHistoryAsync(
                    string.IsNullOrWhiteSpace(FromDate) ? null : FromDate,
                    string.IsNullOrWhiteSpace(ToDate) ? null : ToDate)
                : await _medicineService.ListMovementsAsync();
            IsBusy = false;

            if (!result.Success)
            {
                ErrorMessage = result.ErrorMessage ?? "No se pudo cargar la información";
                return;
            }

            Movements = new ObservableCollection<InventoryMovementDto>(result.Data ?? new());
        }
    }
}