using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DrogueriaMickey.Desktop.Services;

namespace DrogueriaMickey.Desktop.ViewModels.Inventory
{
    public partial class GeneralInventoryViewModel : ObservableObject
    {
        private readonly MedicineService _medicineService;

        [ObservableProperty] private ObservableCollection<InventoryGeneralItemDto> generalItems = new();
        [ObservableProperty] private ObservableCollection<CriticalInventoryItemDto> criticalItems = new();
        [ObservableProperty] private string errorMessage = string.Empty;
        [ObservableProperty] private bool isBusy;

        public GeneralInventoryViewModel(MedicineService medicineService)
        {
            _medicineService = medicineService;
            _ = LoadAll();
        }

        [RelayCommand]
        private async Task LoadAll()
        {
            IsBusy = true;
            var generalResult = await _medicineService.GetGeneralInventoryAsync();
            var criticalResult = await _medicineService.GetCriticalInventoryAsync();
            IsBusy = false;

            if (generalResult.Success) GeneralItems = new ObservableCollection<InventoryGeneralItemDto>(generalResult.Data ?? new());
            if (criticalResult.Success) CriticalItems = new ObservableCollection<CriticalInventoryItemDto>(criticalResult.Data ?? new());

            if (!generalResult.Success || !criticalResult.Success)
                ErrorMessage = "No se pudo cargar toda la información del inventario";
        }
    }
}