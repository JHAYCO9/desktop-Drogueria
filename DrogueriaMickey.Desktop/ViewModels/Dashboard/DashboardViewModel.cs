using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using DrogueriaMickey.Desktop.Navigation;
using DrogueriaMickey.Desktop.Services;

namespace DrogueriaMickey.Desktop.ViewModels.Dashboard
{
    public partial class DashboardViewModel : ObservableObject
    {
        private readonly DashboardService _dashboardService;
        private readonly IServiceProvider _services;
        private readonly NavigationService _navigation;

        [ObservableProperty] private decimal todaySalesTotal;
        [ObservableProperty] private int todaySalesCount;
        [ObservableProperty] private decimal averageTicket;
        [ObservableProperty] private string mostUsedPaymentMethod = string.Empty;

        [ObservableProperty] private int lowStockCount;
        [ObservableProperty] private int outOfStockCount;
        [ObservableProperty] private int expiringSoonCount;
        [ObservableProperty] private int expiredCount;

        [ObservableProperty] private bool cashRegisterOpen;
        [ObservableProperty] private decimal cashBalance;

        [ObservableProperty] private ObservableCollection<ActivityItemDto> recentActivity = new();
        [ObservableProperty] private string errorMessage = string.Empty;
        [ObservableProperty] private bool isBusy;

        public string CashStatusLabel => CashRegisterOpen ? "Abierta" : "Cerrada";

        public DashboardViewModel(DashboardService dashboardService, IServiceProvider services, NavigationService navigation)
        {
            _dashboardService = dashboardService;
            _services = services;
            _navigation = navigation;
            _ = LoadSummary();
        }

        [RelayCommand]
        private async Task LoadSummary()
        {
            ErrorMessage = string.Empty;
            IsBusy = true;
            var result = await _dashboardService.GetSummaryAsync();
            IsBusy = false;

            if (!result.Success || result.Data == null)
            {
                ErrorMessage = result.ErrorMessage ?? "No se pudo cargar el dashboard";
                return;
            }

            var d = result.Data;
            TodaySalesTotal = d.TodaySalesTotal;
            TodaySalesCount = d.TodaySalesCount;
            AverageTicket = d.AverageTicket;
            MostUsedPaymentMethod = string.IsNullOrEmpty(d.MostUsedPaymentMethod) ? "—" : (d.MostUsedPaymentMethod == "nequi" ? "Nequi" : "Efectivo");

            LowStockCount = d.LowStockCount;
            OutOfStockCount = d.OutOfStockCount;
            ExpiringSoonCount = d.ExpiringSoonCount;
            ExpiredCount = d.ExpiredCount;

            CashRegisterOpen = d.CashRegisterOpen;
            OnPropertyChanged(nameof(CashStatusLabel));
            CashBalance = d.CashBalance;

            RecentActivity = new ObservableCollection<ActivityItemDto>(d.RecentActivity);
        }

        // RF-059: clic en cualquier indicador de inventario lleva al detalle
        [RelayCommand]
        private void GoToAlerts()
        {
            var vm = _services.GetRequiredService<ViewModels.Inventory.StockAlertsViewModel>();
            var view = new Views.Inventory.StockAlertsView { DataContext = vm };
            _navigation.NavigateTo(view);
        }

        [RelayCommand]
        private void GoToMedicines()
        {
            var vm = _services.GetRequiredService<ViewModels.Inventory.MedicinesListViewModel>();
            var view = new Views.Inventory.MedicinesListView { DataContext = vm };
            _navigation.NavigateTo(view);
        }

        [RelayCommand]
        private void GoToCashRegister()
        {
            var vm = _services.GetRequiredService<ViewModels.CashRegister.CashRegisterViewModel>();
            var view = new Views.CashRegister.CashRegisterView { DataContext = vm };
            _navigation.NavigateTo(view);
        }

        [RelayCommand]
        private void GoToSalesHistory()
        {
            var vm = _services.GetRequiredService<ViewModels.Sales.SalesHistoryViewModel>();
            var view = new Views.Sales.SalesHistoryView { DataContext = vm };
            _navigation.NavigateTo(view);
        }
    }
}