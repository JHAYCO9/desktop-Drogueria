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
        private bool _isShowingError;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Red de seguridad: cualquier excepción no atrapada en cualquier
            // parte de la app se captura aquí, se muestra un mensaje, y la
            // aplicación sigue viva en vez de cerrarse de golpe.
            DispatcherUnhandledException += (s, args) =>
            {
                args.Handled = true; // siempre marcamos manejado primero, para no tumbar la app

                if (_isShowingError) return; // evita la cascada: si ya hay un diálogo abierto, ignora el resto

                _isShowingError = true;
                try
                {
                    System.Windows.MessageBox.Show(
                        $"Ocurrió un error inesperado:\n\n{args.Exception.Message}\n\nLa aplicación continuará funcionando.",
                        "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                }
                finally
                {
                    _isShowingError = false;
                }
            };

            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                var ex = args.ExceptionObject as Exception;
                System.Windows.MessageBox.Show(
                    $"Error crítico: {ex?.Message}",
                    "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            };

            TaskScheduler.UnobservedTaskException += (s, args) =>
            {
                args.SetObserved(); // evita que una excepción de una Task en segundo plano tumbe el proceso
            };

            var services = new ServiceCollection();

            services.AddHttpClient<SaleService>();
            services.AddSingleton<ReceiptPrintService>();
            services.AddSingleton<CashDrawerService>();
            services.AddTransient<ViewModels.Sales.SalePosViewModel>();
            services.AddTransient<ViewModels.Inventory.MedicineSearchViewModel>();
            services.AddTransient<ViewModels.Inventory.InventoryMovementsViewModel>();
            services.AddTransient<ViewModels.Inventory.GeneralInventoryViewModel>();
            services.AddTransient<ViewModels.Inventory.StockAlertsViewModel>();
            services.AddTransient<ViewModels.Inventory.BatchesViewModel>();
            services.AddHttpClient<MedicineService>();
            services.AddTransient<ViewModels.Inventory.MedicinesListViewModel>();
            services.AddTransient<ViewModels.Inventory.MedicineEditViewModel>();
            services.AddTransient<ViewModels.Users.AuditLogViewModel>();
            services.AddTransient<ViewModels.Auth.ChangePasswordViewModel>();
            services.AddSingleton<SessionService>();
            services.AddSingleton<Navigation.NavigationService>();
            services.AddDbContext<AppDbContext>(contextLifetime: ServiceLifetime.Transient, optionsLifetime: ServiceLifetime.Transient); services.AddHttpClient<AuthService>();
            services.AddSingleton<SyncService>();
            services.AddTransient<ViewModels.Sales.SalesHistoryViewModel>();
            services.AddTransient<ViewModels.Auth.LoginViewModel>();
            services.AddTransient<ViewModels.MainViewModel>();
            services.AddHttpClient<UserService>();
            services.AddTransient<ViewModels.Users.UsersListViewModel>();
            services.AddTransient<ViewModels.Users.UserEditViewModel>();
            services.AddSingleton<ReceiptPrintService>();
            services.AddTransient<ViewModels.Sales.SaleDetailViewModel>();
            services.AddHttpClient<CashRegisterService>();
            services.AddTransient<ViewModels.CashRegister.CashRegisterViewModel>();
            services.AddTransient<ViewModels.CashRegister.CashMovementsViewModel>();
            services.AddHttpClient<DashboardService>();
            services.AddTransient<ViewModels.Dashboard.DashboardViewModel>();
            services.AddSingleton<ReporteZPrintService>();
            services.AddHttpClient<ReturnService>();
            services.AddTransient<ViewModels.Returns.ReturnCreateViewModel>();
            services.AddTransient<ViewModels.Returns.ReturnsHistoryViewModel>();

            ServiceProvider = services.BuildServiceProvider();

            using (var scope = ServiceProvider.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Database.Migrate();
                db.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");
                db.Database.ExecuteSqlRaw("PRAGMA busy_timeout=5000;"); // espera hasta 5s si hay un lock, en vez de fallar de inmediato
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