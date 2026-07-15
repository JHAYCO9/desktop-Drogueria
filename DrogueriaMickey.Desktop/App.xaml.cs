using DrogueriaMickey.Desktop;
using DrogueriaMickey.Desktop.Data;
using DrogueriaMickey.Desktop.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace DrogueriaMickey.Desktop
{
    public partial class App : Application
    {
        public static IServiceProvider ServiceProvider { get; private set; } = null!;
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var services = new ServiceCollection();

            services.AddTransient<ViewModels.Inventory.StockAlertsViewModel>();
            services.AddTransient<ViewModels.Inventory.BatchesViewModel>();
            services.AddHttpClient<MedicineService>();
            services.AddTransient<ViewModels.Inventory.MedicinesListViewModel>();
            services.AddTransient<ViewModels.Inventory.MedicineEditViewModel>();
            services.AddTransient<ViewModels.Users.AuditLogViewModel>();
            services.AddTransient<ViewModels.Auth.ChangePasswordViewModel>();
            services.AddSingleton<SessionService>();
            services.AddSingleton<Navigation.NavigationService>();
            services.AddDbContext<AppDbContext>();
            services.AddHttpClient<AuthService>();
            services.AddSingleton<SyncService>();
            services.AddTransient<ViewModels.Auth.LoginViewModel>();
            services.AddTransient<ViewModels.MainViewModel>();
            services.AddHttpClient<UserService>();
            services.AddTransient<ViewModels.Users.UsersListViewModel>();
            services.AddTransient<ViewModels.Users.UserEditViewModel>();

            ServiceProvider = services.BuildServiceProvider();

            using (var scope = ServiceProvider.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Database.Migrate();
            }

            var mainWindow = new MainWindow();
            ShowLogin(mainWindow);
            mainWindow.Show();
        }

        private void ShowLogin(MainWindow mainWindow)
        {
            var loginViewModel = ServiceProvider.GetRequiredService<ViewModels.Auth.LoginViewModel>();
            loginViewModel.LoginSucceeded += () => ShowShell(mainWindow);

            var loginView = new Views.Auth.LoginView { DataContext = loginViewModel };
            mainWindow.MainContent.Content = loginView;
        }

        private void ShowShell(MainWindow mainWindow)
        {
            var mainViewModel = ServiceProvider.GetRequiredService<ViewModels.MainViewModel>();
            mainViewModel.LoggedOut += () => ShowLogin(mainWindow);

            var shellView = new Views.Shell.ShellView { DataContext = mainViewModel };
            mainWindow.MainContent.Content = shellView;
        }
    }
}