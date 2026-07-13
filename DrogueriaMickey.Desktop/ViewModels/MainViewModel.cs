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

        public MainViewModel(IServiceProvider services, SessionService session, NavigationService navigation, AuthService authService) // <-- CAMBIO 2: parámetro nuevo
        {
            _services = services;
            _session = session;
            Navigation = navigation;
            _authService = authService;                              // <-- CAMBIO 2: asignación nueva
            // vista inicial al entrar al shell
            NavigateToDashboard();
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
            var view = new Views.Inventory.MedicinesListView();
            Navigation.NavigateTo(view);
        }

        [RelayCommand]
        private void NavigateToSales()
        {
            var view = new Views.Sales.SalePosView();
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
    }
}