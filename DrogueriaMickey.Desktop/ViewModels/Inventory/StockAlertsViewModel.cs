using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DrogueriaMickey.Desktop.Services;

namespace DrogueriaMickey.Desktop.ViewModels.Inventory
{
    public partial class StockAlertsViewModel : ObservableObject
    {
        private readonly MedicineService _medicineService;

        [ObservableProperty] private ObservableCollection<CriticalMedicineDto> criticalMedicines = new();
        [ObservableProperty] private ObservableCollection<BatchDto> expiringSoon = new();
        [ObservableProperty] private ObservableCollection<BatchDto> expired = new();
        [ObservableProperty] private string errorMessage = string.Empty;
        [ObservableProperty] private bool isBusy;

        public StockAlertsViewModel(MedicineService medicineService)
        {
            _medicineService = medicineService;
            _ = LoadAll();
        }

        [RelayCommand]
        private async Task LoadAll()
        {
            IsBusy = true;
            ErrorMessage = string.Empty;

            var criticalResult = await _medicineService.ListCriticalMedicinesAsync();
            var expiringResult = await _medicineService.ListExpiringSoonAsync(30);
            var expiredResult = await _medicineService.ListExpiredAsync();

            IsBusy = false;

            if (criticalResult.Success) CriticalMedicines = new ObservableCollection<CriticalMedicineDto>(criticalResult.Data ?? new());
            if (expiringResult.Success) ExpiringSoon = new ObservableCollection<BatchDto>(expiringResult.Data ?? new());
            if (expiredResult.Success) Expired = new ObservableCollection<BatchDto>(expiredResult.Data ?? new());

            if (!criticalResult.Success || !expiringResult.Success || !expiredResult.Success)
                ErrorMessage = "Algunas alertas no se pudieron cargar";
        }
    }
}