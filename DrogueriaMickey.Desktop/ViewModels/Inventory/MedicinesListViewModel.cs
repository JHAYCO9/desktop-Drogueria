using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using DrogueriaMickey.Desktop.Navigation;
using DrogueriaMickey.Desktop.Services;

namespace DrogueriaMickey.Desktop.ViewModels.Inventory
{
    public partial class MedicinesListViewModel : ObservableObject
    {
        private readonly MedicineService _medicineService;
        private readonly IServiceProvider _services;
        private readonly NavigationService _navigation;

        private List<MedicineDto> _allMedicines = new(); // copia completa, sin filtrar

        [ObservableProperty] private ObservableCollection<MedicineDto> medicines = new();
        [ObservableProperty] private string barcodeInput = string.Empty;
        [ObservableProperty] private string errorMessage = string.Empty;
        [ObservableProperty] private bool isFiltered; // controla si se ve el botón "Ver todos"
        [ObservableProperty] private bool isBusy;

        public MedicinesListViewModel(MedicineService medicineService, IServiceProvider services, NavigationService navigation)
        {
            _medicineService = medicineService;
            _services = services;
            _navigation = navigation;
            _ = LoadMedicines();
        }

        [RelayCommand]
        private async Task LoadMedicines()
        {
            IsBusy = true;
            var result = await _medicineService.ListMedicinesAsync();
            IsBusy = false;

            if (!result.Success)
            {
                ErrorMessage = result.ErrorMessage ?? "No se pudieron cargar los medicamentos";
                return;
            }

            _allMedicines = result.Data ?? new();
            Medicines = new ObservableCollection<MedicineDto>(_allMedicines);
            IsFiltered = false;
        }

        // RF-026/027: filtra la tabla al medicamento escaneado, en vez de
        // solo mostrar un mensaje aparte.
        [RelayCommand]
        private async Task ScanBarcode()
        {
            ErrorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(BarcodeInput))
                return;

            IsBusy = true;
            var result = await _medicineService.GetByBarcodeAsync(BarcodeInput.Trim());
            IsBusy = false;

            if (!result.Success || result.Data == null)
            {
                ErrorMessage = $"No se encontró ningún medicamento con el código {BarcodeInput}";
                BarcodeInput = string.Empty;
                return;
            }

            Medicines = new ObservableCollection<MedicineDto> { result.Data };
            IsFiltered = true;
            BarcodeInput = string.Empty;
        }

        [RelayCommand]
        private void ClearFilter()
        {
            Medicines = new ObservableCollection<MedicineDto>(_allMedicines);
            IsFiltered = false;
            ErrorMessage = string.Empty;
        }

        [RelayCommand]
        private void ViewBatches(MedicineDto medicine)
        {
            var vm = _services.GetRequiredService<BatchesViewModel>();
            vm.Initialize(medicine.Id, medicine.Name);
            var view = new Views.Inventory.BatchesView { DataContext = vm };
            _navigation.NavigateTo(view);
        }

        [RelayCommand]
        private async Task ConfigureAlert(MedicineDto medicine)
        {
            var input = Microsoft.VisualBasic.Interaction.InputBox(
                "Días antes del vencimiento para alertar:", "Configurar alerta", "30");

            if (int.TryParse(input, out var days) && days > 0)
            {
                var result = await _medicineService.UpdateAlertDaysAsync(medicine.Id, days);
                if (!result.Success)
                    ErrorMessage = result.ErrorMessage ?? "No se pudo actualizar la configuración";
            }
        }

        [RelayCommand]
        private void NewMedicine()
        {
            var vm = _services.GetRequiredService<MedicineEditViewModel>();
            vm.Saved += async () =>
            {
                // vuelve a la lista y la recarga con el medicamento nuevo incluido
                var listView = new Views.Inventory.MedicinesListView { DataContext = this };
                _navigation.NavigateTo(listView);
                await LoadMedicines();
            };
            var view = new Views.Inventory.MedicineEditView { DataContext = vm };
            _navigation.NavigateTo(view);
        }
    }
}