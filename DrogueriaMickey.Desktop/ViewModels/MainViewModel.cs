using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DrogueriaMickey.Desktop.Navigation;
using DrogueriaMickey.Desktop.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Controls;

namespace DrogueriaMickey.Desktop.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly IServiceProvider _services;
        private readonly SessionService _session;
        private readonly AuthService _authService;                  // <-- CAMBIO 1: nuevo campo
        public NavigationService Navigation { get; }
        public string Username => _session.Username ?? string.Empty;
        public string RoleName => _session.RoleName ?? string.Empty;
        public bool IsAdmin => _session.IsAdministrator;
        public bool IsOfflineSession => _session.IsOfflineSession;
        public event Action? LoggedOut;

        private readonly SyncService _syncService;
        public SyncService Sync => _syncService;

        public MainViewModel(IServiceProvider services, SessionService session, NavigationService navigation, AuthService authService, SyncService syncService)
        {
            _services = services;
            _session = session;
            Navigation = navigation;
            _authService = authService;
            _syncService = syncService;
            NavigateToDashboard();
        }

        [RelayCommand]
        private async Task SyncNow()
        {
            await _syncService.SyncNowAsync();
        }

        [RelayCommand]
        private void NavigateToDashboard()
        {
            var view = new Views.Dashboard.DashboardView(); // sin ViewModel propio todavía, ok por ahora
            Navigation.NavigateTo(view);
        }
            
        [RelayCommand]
        private void NavigateToUsers()
        {
            if (!IsAdmin)
            {
                System.Windows.MessageBox.Show("No tienes permisos para acceder a este módulo.", "Acceso restringido");
                return;
            }

            var vm = _services.GetRequiredService<ViewModels.Users.UsersListViewModel>();
            var view = new Views.Users.UsersListView { DataContext = vm };
            Navigation.NavigateTo(view);
        }

        [RelayCommand]
        private void NavigateToMedicines()
        {
            var vm = _services.GetRequiredService<ViewModels.Inventory.MedicinesListViewModel>();
            var view = new Views.Inventory.MedicinesListView { DataContext = vm };
            Navigation.NavigateTo(view);
        }

        [RelayCommand]
        private void NavigateToStockAlerts()
        {
            var vm = _services.GetRequiredService<ViewModels.Inventory.StockAlertsViewModel>();
            var view = new Views.Inventory.StockAlertsView { DataContext = vm };
            Navigation.NavigateTo(view);
        }

        [RelayCommand]
        private void NavigateToSales()
        {
            var vm = _services.GetRequiredService<ViewModels.Sales.SalePosViewModel>();
            var view = new Views.Sales.SalePosView { DataContext = vm };
            Navigation.NavigateTo(view);
        }

        [RelayCommand]
        private void NavigateToCashRegister()
        {
            var view = new Views.CashRegister.CashStatusView();
            Navigation.NavigateTo(view);
        }

        [RelayCommand]
        private async Task Logout()                                  // <-- CAMBIO 3: era "private void", ahora "private async Task"
        {
            await _authService.LogoutAsync();                        // <-- CAMBIO 3: llama al backend primero
            LoggedOut?.Invoke();
        }


        [RelayCommand]
        private void NavigateToChangePassword()
        {
            var vm = _services.GetRequiredService<ViewModels.Auth.ChangePasswordViewModel>();
            var view = new Views.Auth.ChangePasswordView { DataContext = vm };
            Navigation.NavigateTo(view);
        }

        [RelayCommand]
        private void NavigateToAuditLog()
        {
            if (!IsAdmin)
            {
                System.Windows.MessageBox.Show("No tienes permisos para acceder a este módulo.", "Acceso restringido");
                return;
            }
            var vm = _services.GetRequiredService<ViewModels.Users.AuditLogViewModel>();
            var view = new Views.Users.AuditLogView { DataContext = vm };
            Navigation.NavigateTo(view);
        }

        [RelayCommand]
        private void NavigateToMovements()
        {
            var vm = _services.GetRequiredService<ViewModels.Inventory.InventoryMovementsViewModel>();
            vm.InitializeMovements();
            var view = new Views.Inventory.InventoryMovementsView { DataContext = vm };
            Navigation.NavigateTo(view);
        }

        [RelayCommand]
        private void NavigateToSalesHistory()
        {
            if (!IsAdmin)
            {
                System.Windows.MessageBox.Show("No tienes permisos para acceder a este módulo.", "Acceso restringido");
                return;
            }
            var vm = _services.GetRequiredService<ViewModels.Sales.SalesHistoryViewModel>();
            var view = new Views.Sales.SalesHistoryView { DataContext = vm };
            Navigation.NavigateTo(view);
        }

        [RelayCommand]
        private void NavigateToStockHistory()
        {
            var vm = _services.GetRequiredService<ViewModels.Inventory.InventoryMovementsViewModel>();
            vm.InitializeHistory();
            var view = new Views.Inventory.InventoryMovementsView { DataContext = vm };
            Navigation.NavigateTo(view);
        }

    }
}