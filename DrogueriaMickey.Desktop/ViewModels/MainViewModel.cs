using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DrogueriaMickey.Desktop.Navigation;
using DrogueriaMickey.Desktop.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;
using System.Threading;
using System.Windows.Controls;

namespace DrogueriaMickey.Desktop.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly IServiceProvider _services;
        private readonly SessionService _session;
        private readonly AuthService _authService;
        public NavigationService Navigation { get; }
        public string Username => _session.Username ?? string.Empty;
        public string RoleName => _session.RoleName ?? string.Empty;
        public bool IsAdmin => _session.IsAdministrator;
        public bool IsOfflineSession => _session.IsOfflineSession;
        public event Action? LoggedOut;
        private readonly SyncService _syncService;
        public SyncService Sync => _syncService;
        public ViewModels.Bot.BotChatViewModel Bot { get; }

        // Mantiene el backend en Render "despierto" — evita que el
        // servicio se duerma entre ventas y cause timeouts largos o el
        // error "no se pudo contactar con el servidor" al primer intento
        // después de un rato de inactividad.
        private readonly Timer _keepAliveTimer;

        public MainViewModel(IServiceProvider services, SessionService session, NavigationService navigation, AuthService authService, SyncService syncService)
        {
            _services = services;
            _session = session;
            Navigation = navigation;
            _authService = authService;
            _syncService = syncService;
            Bot = _services.GetRequiredService<ViewModels.Bot.BotChatViewModel>();
            NavigateToDashboard();

            _keepAliveTimer = new Timer(async _ => await PingHealthAsync(), null, TimeSpan.FromMinutes(4), TimeSpan.FromMinutes(4));
        }

        private static async Task PingHealthAsync()
        {
            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
                await client.GetAsync($"{AppConfig.ApiBaseUrl}health");
            }
            catch
            {
                // Si falla, no pasa nada — es solo mantenimiento en
                // segundo plano, no debe interrumpir al usuario.
            }
        }

        [RelayCommand]
        private async Task SyncNow()
        {
            await _syncService.SyncNowAsync();
        }

        [RelayCommand]
        private void NavigateToDashboard()
        {
            var vm = _services.GetRequiredService<ViewModels.Dashboard.DashboardViewModel>();
            var view = new Views.Dashboard.DashboardView { DataContext = vm };
            Navigation.NavigateTo(view);
        }

        [RelayCommand]
        private void NavigateToSettings()
        {
            var vm = _services.GetRequiredService<ViewModels.Settings.SettingsViewModel>();
            var view = new Views.Settings.SettingsView { DataContext = vm };
            Navigation.NavigateTo(view);
        }

        [RelayCommand]
        private void NavigateToReturnsHistory()
        {
            var vm = _services.GetRequiredService<ViewModels.Returns.ReturnsHistoryViewModel>();
            var view = new Views.Returns.ReturnsHistoryView { DataContext = vm };
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
            var vm = _services.GetRequiredService<ViewModels.CashRegister.CashRegisterViewModel>();
            var view = new Views.CashRegister.CashRegisterView { DataContext = vm };
            Navigation.NavigateTo(view);
        }

        [RelayCommand]
        private async Task Logout()
        {
            _keepAliveTimer.Change(Timeout.Infinite, Timeout.Infinite);
            await _authService.LogoutAsync();
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
        private void NavigateToReports()
        {
            var vm = _services.GetRequiredService<ViewModels.Reports.ReportsViewModel>();
            var view = new Views.Reports.ReportsView { DataContext = vm };
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