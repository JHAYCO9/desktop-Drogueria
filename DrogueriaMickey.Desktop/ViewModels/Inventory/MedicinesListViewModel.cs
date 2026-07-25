using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using DrogueriaMickey.Desktop.Navigation;
using DrogueriaMickey.Desktop.Services;
using System.Linq;

namespace DrogueriaMickey.Desktop.ViewModels.Inventory
{
    public partial class MedicinesListViewModel : ObservableObject
    {
        private readonly MedicineService _medicineService;
        private readonly IServiceProvider _services;
        private readonly NavigationService _navigation;

        private List<MedicineDto> _allMedicines = new();

        [ObservableProperty] private ObservableCollection<MedicineDto> medicines = new();
        [ObservableProperty] private string barcodeInput = string.Empty;
        [ObservableProperty] private string filterName = string.Empty;
        [ObservableProperty] private string filterLaboratory = string.Empty;
        [ObservableProperty] private string errorMessage = string.Empty;
        [ObservableProperty] private bool isFiltered;
        [ObservableProperty] private bool isBusy;

        private readonly SessionService _session;

        public bool IsAdmin => _session.IsAdministrator;

        public MedicinesListViewModel(MedicineService medicineService, IServiceProvider services, NavigationService navigation, SessionService session)
        {
            _medicineService = medicineService;
            _services = services;
            _navigation = navigation;
            _session = session;
            _ = LoadMedicines();
        }

        public MedicinesListViewModel(MedicineService medicineService, IServiceProvider services, NavigationService navigation)
        {
            _medicineService = medicineService;
            _services = services;
            _navigation = navigation;
            _ = LoadMedicines();
        }

        // Carga la lista base de medicamentos y la enriquece con stock/estado
        // desde inventario general (RF-028), para tener todo en una sola tabla.
        [RelayCommand]
        private async Task LoadMedicines()
        {
            IsBusy = true;
            ErrorMessage = string.Empty;

            var medsResult = await _medicineService.ListMedicinesAsync();
            var generalResult = await _medicineService.GetGeneralInventoryAsync();
            IsBusy = false;

            if (!medsResult.Success)
            {
                ErrorMessage = medsResult.ErrorMessage ?? "No se pudieron cargar los medicamentos";
                return;
            }

            var list = medsResult.Data ?? new List<MedicineDto>();
            var generalById = (generalResult.Data ?? new List<InventoryGeneralItemDto>())
                .ToDictionary(g => g.Id, g => g);

            foreach (var m in list)
            {
                if (generalById.TryGetValue(m.Id, out var g))
                {
                    m.TotalStock = g.TotalStock;
                    m.Status = g.Status;
                    m.NextExpiration = g.NextExpiration;
                    m.AvailableBatches = g.AvailableBatches;
                }
            }

            _allMedicines = list;
            ApplyFilter();
            IsFiltered = false;
        }

        partial void OnFilterNameChanged(string value) => ApplyFilter();
        partial void OnFilterLaboratoryChanged(string value) => ApplyFilter();

        private void ApplyFilter()
        {
            IEnumerable<MedicineDto> filtered = _allMedicines;

            if (!string.IsNullOrWhiteSpace(FilterName))
                filtered = filtered.Where(m => m.Name.Contains(FilterName, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(FilterLaboratory))
                filtered = filtered.Where(m => m.Laboratory.Contains(FilterLaboratory, StringComparison.OrdinalIgnoreCase));

            Medicines = new ObservableCollection<MedicineDto>(filtered);
        }

        // RF-026/027: escaneo físico, exacto, contra el backend.
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
            FilterName = string.Empty;
            FilterLaboratory = string.Empty;
            ApplyFilter();
            IsFiltered = false;
            ErrorMessage = string.Empty;
        }

        [RelayCommand]
        private void NewMedicine()
        {
            var vm = _services.GetRequiredService<MedicineEditViewModel>();
            vm.InitializeCreate();
            vm.Saved += async () =>
            {
                var listView = new Views.Inventory.MedicinesListView { DataContext = this };
                _navigation.NavigateTo(listView);
                await LoadMedicines();
            };
            var view = new Views.Inventory.MedicineEditView { DataContext = vm };
            _navigation.NavigateTo(view);
        }

        [RelayCommand]
        private void EditMedicine(MedicineDto medicine)
        {
            var vm = _services.GetRequiredService<MedicineEditViewModel>();
            vm.InitializeEdit(medicine);
            vm.Saved += async () =>
            {
                var listView = new Views.Inventory.MedicinesListView { DataContext = this };
                _navigation.NavigateTo(listView);
                await LoadMedicines();
            };
            var view = new Views.Inventory.MedicineEditView { DataContext = vm };
            _navigation.NavigateTo(view);
        }

        [RelayCommand]
        private async Task ToggleActive(MedicineDto medicine)
        {
            var action = medicine.IsActive ? "desactivar" : "activar";
            var confirm = System.Windows.MessageBox.Show(
                $"¿Seguro que quieres {action} {medicine.Name}?", "Confirmar",
                System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);

            if (confirm != System.Windows.MessageBoxResult.Yes) return;

            IsBusy = true;
            var result = medicine.IsActive
                ? await _medicineService.DeactivateMedicineAsync(medicine.Id)
                : await _medicineService.ActivateMedicineAsync(medicine.Id);
            IsBusy = false;

            if (!result.Success)
            {
                ErrorMessage = result.ErrorMessage ?? $"No se pudo {action} el medicamento";
                return;
            }

            await LoadMedicines();
        }

        [RelayCommand]
        private void ViewBatches(MedicineDto medicine)
        {
            var vm = _services.GetRequiredService<BatchesViewModel>();
            vm.Initialize(medicine.Id, medicine.Name);
            var view = new Views.Inventory.BatchesView { DataContext = vm };
            _navigation.NavigateTo(view);
        }

        // Botón dentro de la misma vista de Medicamentos, pide el ViewModel
        // de movimientos ya construido (RF-019) y navega a él.
        [RelayCommand]
        private void ViewMovements()
        {
            var vm = _services.GetRequiredService<InventoryMovementsViewModel>();
            vm.InitializeMovements();
            var view = new Views.Inventory.InventoryMovementsView { DataContext = vm };
            _navigation.NavigateTo(view);
        }
    }
}