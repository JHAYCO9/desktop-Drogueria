using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DrogueriaMickey.Desktop.Services;

namespace DrogueriaMickey.Desktop.ViewModels.Inventory
{
    public partial class MedicineSearchViewModel : ObservableObject
    {
        private readonly MedicineService _medicineService;

        [ObservableProperty] private string nameFilter = string.Empty;
        [ObservableProperty] private string barcodeFilter = string.Empty;
        [ObservableProperty] private string laboratoryFilter = string.Empty;
        [ObservableProperty] private ObservableCollection<MedicineSearchDto> results = new();
        [ObservableProperty] private string errorMessage = string.Empty;
        [ObservableProperty] private bool isBusy;

        public MedicineSearchViewModel(MedicineService medicineService)
        {
            _medicineService = medicineService;
        }

        [RelayCommand]
        private async Task Search()
        {
            ErrorMessage = string.Empty;
            IsBusy = true;
            var result = await _medicineService.SearchMedicinesAsync(NameFilter, BarcodeFilter, LaboratoryFilter);
            IsBusy = false;

            if (!result.Success)
            {
                ErrorMessage = result.ErrorMessage ?? "No se pudo realizar la búsqueda";
                return;
            }

            Results = new ObservableCollection<MedicineSearchDto>(result.Data ?? new());
        }
    }
}