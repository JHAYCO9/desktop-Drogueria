using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DrogueriaMickey.Desktop.Services;
using System.Collections.ObjectModel;
using static DrogueriaMickey.Desktop.Services.BotService;

namespace DrogueriaMickey.Desktop.ViewModels.Settings
{
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly SystemSettingsService _settingsService;
        private readonly BotService _botService;
        private readonly UserService _userService;

        // RF-079
        [ObservableProperty] private string businessName = string.Empty;
        [ObservableProperty] private string businessNit = string.Empty;
        [ObservableProperty] private string businessAddress = string.Empty;
        [ObservableProperty] private string businessPhone = string.Empty;
        [ObservableProperty] private string businessEmail = string.Empty;

        // RF-080
        [ObservableProperty] private string expirationAlertDaysInput = "30";
        [ObservableProperty] private bool expirationAlertEnabled = true;

        // RF-081
        [ObservableProperty] private bool stockAlertEnabled = true;
        [ObservableProperty] private string stockAlertFrequencyHoursInput = "24";
        [ObservableProperty] private string stockAlertStartTime = "08:00";
        [ObservableProperty] private string stockAlertEndTime = "18:00";

        // RF-083
        [ObservableProperty] private string reporteZHour = "20:00";

        // RF-084
        [ObservableProperty] private string lastBackupAt = "Nunca";

        // RF-082: config del bot
        [ObservableProperty] private bool botEnabled = true;
        [ObservableProperty] private string botWelcomeMessage = string.Empty;

        // RF-070: usuarios con acceso al bot
        [ObservableProperty] private ObservableCollection<UserListItemDto> allUsers = new();
        [ObservableProperty] private ObservableCollection<string> botAuthorizedUserIds = new();
        [ObservableProperty] private ObservableCollection<BotAuthorizedUserDto> botAuthorizedUsers = new();
        [ObservableProperty] private UserListItemDto? selectedUserToGrant;
        [ObservableProperty] private ObservableCollection<BotQueryLogDto> botQueryLogs = new();

        [ObservableProperty] private string errorMessage = string.Empty;
        [ObservableProperty] private string successMessage = string.Empty;
        [ObservableProperty] private bool isBusy;

        // RF-086
        [ObservableProperty] private string legalName = string.Empty;
        [ObservableProperty] private string taxRegime = string.Empty;
        [ObservableProperty] private string fiscalAddress = string.Empty;

        public SettingsViewModel(SystemSettingsService settingsService, BotService botService, UserService userService)
        {
            _settingsService = settingsService;
            _botService = botService;
            _userService = userService;
            _ = LoadAll();
        }

        [RelayCommand]
        private async Task LoadAll()
        {
            ErrorMessage = string.Empty;
            IsBusy = true;

            var settingsResult = await _settingsService.GetAsync();
            if (settingsResult.Success && settingsResult.Data != null)
            {
                var d = settingsResult.Data;
                BusinessName = d.BusinessName;
                BusinessNit = d.BusinessNit;
                BusinessAddress = d.BusinessAddress;
                BusinessPhone = d.BusinessPhone;
                BusinessEmail = d.BusinessEmail;
                LegalName = d.LegalName;
                TaxRegime = d.TaxRegime;
                FiscalAddress = d.FiscalAddress;
                ExpirationAlertDaysInput = d.ExpirationAlertDays.ToString();
                ExpirationAlertEnabled = d.ExpirationAlertEnabled;
                StockAlertEnabled = d.StockAlertEnabled;
                StockAlertFrequencyHoursInput = d.StockAlertFrequencyHours.ToString();
                StockAlertStartTime = d.StockAlertStartTime;
                StockAlertEndTime = d.StockAlertEndTime;
                ReporteZHour = d.ReporteZHour;
                LastBackupAt = string.IsNullOrEmpty(d.LastBackupAt) ? "Nunca" : d.LastBackupAt;
            }

            var botConfigResult = await _botService.GetConfigAsync();
            if (botConfigResult.Success && botConfigResult.Data != null)
            {
                BotEnabled = botConfigResult.Data.IsEnabled;
                BotWelcomeMessage = botConfigResult.Data.WelcomeMessage;
            }

            var usersResult = await _userService.ListUsersAsync();
            if (usersResult.Success)
            {
                AllUsers = new ObservableCollection<UserListItemDto>(usersResult.Data ?? new());
            }

            var authorizedResult = await _botService.ListAuthorizedAsync();
            if (authorizedResult.Success)
            {
                BotAuthorizedUsers = new ObservableCollection<BotAuthorizedUserDto>(authorizedResult.Data ?? new());
            }
            var logsResult = await _botService.ListQueryLogsAsync();
            if (logsResult.Success)
            {
                BotQueryLogs = new ObservableCollection<BotQueryLogDto>(logsResult.Data ?? new());
            }

            IsBusy = false;
        }

        // RF-079
        [RelayCommand]
        private async Task SaveBusinessInfo()
        {
            ErrorMessage = string.Empty;
            SuccessMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(BusinessName))
            {
                ErrorMessage = "El nombre de la droguería es obligatorio";
                return;
            }

            IsBusy = true;
            var result = await _settingsService.UpdateBusinessInfoAsync(BusinessName, BusinessNit, BusinessAddress, BusinessPhone, BusinessEmail);
            IsBusy = false;

            if (!result.Success)
            {
                ErrorMessage = result.ErrorMessage ?? "No se pudo guardar la información";
                return;
            }
            SuccessMessage = "Información de la droguería actualizada";
        }

        // RF-086
        [RelayCommand]
        private async Task SaveTaxSettings()
        {
            ErrorMessage = string.Empty;
            SuccessMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(BusinessNit) || string.IsNullOrWhiteSpace(LegalName))
            {
                ErrorMessage = "El NIT y la razón social son obligatorios";
                return;
            }

            IsBusy = true;
            var result = await _settingsService.UpdateTaxSettingsAsync(BusinessNit, LegalName, TaxRegime, FiscalAddress);
            IsBusy = false;

            if (!result.Success)
            {
                ErrorMessage = result.ErrorMessage ?? "No se pudo guardar la información tributaria";
                return;
            }
            SuccessMessage = "Información tributaria actualizada";
        }

        // RF-082
        [RelayCommand]
        private async Task SaveBotConfig()
        {
            ErrorMessage = string.Empty;
            SuccessMessage = string.Empty;

            IsBusy = true;
            var result = await _botService.UpdateConfigAsync(BotEnabled, BotWelcomeMessage);
            IsBusy = false;

            if (!result.Success)
            {
                ErrorMessage = result.ErrorMessage ?? "No se pudo guardar la configuración del bot";
                return;
            }
            SuccessMessage = "Configuración del bot actualizada";
        }

        // RF-070
        [RelayCommand]
        private async Task GrantBotAccess()
        {
            ErrorMessage = string.Empty;
            SuccessMessage = string.Empty;

            if (SelectedUserToGrant == null)
            {
                ErrorMessage = "Selecciona un usuario para dar acceso";
                return;
            }

            IsBusy = true;
            var result = await _botService.GrantAccessAsync(SelectedUserToGrant.Id);
            IsBusy = false;

            if (!result.Success)
            {
                ErrorMessage = result.ErrorMessage ?? "No se pudo otorgar el acceso";
                return;
            }

            SuccessMessage = "Acceso otorgado correctamente";
            SelectedUserToGrant = null;
            await ReloadAuthorizedUsers();
        }

        [RelayCommand]
        private async Task RevokeBotAccess(BotAuthorizedUserDto user)
        {
            ErrorMessage = string.Empty;
            SuccessMessage = string.Empty;

            var confirm = System.Windows.MessageBox.Show(
                $"¿Revocar el acceso al bot de {user.FullName}?",
                "Confirmar", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning);

            if (confirm != System.Windows.MessageBoxResult.Yes) return;

            IsBusy = true;
            var result = await _botService.RevokeAccessAsync(user.Id);
            IsBusy = false;

            if (!result.Success)
            {
                ErrorMessage = result.ErrorMessage ?? "No se pudo revocar el acceso";
                return;
            }

            SuccessMessage = "Acceso revocado correctamente";
            await ReloadAuthorizedUsers();
        }

        private async Task ReloadAuthorizedUsers()
        {
            var result = await _botService.ListAuthorizedAsync();
            if (result.Success)
            {
                BotAuthorizedUsers = new ObservableCollection<BotAuthorizedUserDto>(result.Data ?? new());
            }
        }

        // RF-080
        [RelayCommand]
        private async Task SaveExpirationSettings()
        {
            ErrorMessage = string.Empty;
            SuccessMessage = string.Empty;

            if (!int.TryParse(ExpirationAlertDaysInput, out var days) || days < 1)
            {
                ErrorMessage = "Los días de anticipación deben ser un número mayor o igual a 1";
                return;
            }

            IsBusy = true;
            var result = await _settingsService.UpdateExpirationSettingsAsync(days, ExpirationAlertEnabled);
            IsBusy = false;

            if (!result.Success)
            {
                ErrorMessage = result.ErrorMessage ?? "No se pudo guardar la configuración";
                return;
            }
            SuccessMessage = "Configuración de vencimientos actualizada";
        }

        // RF-081
        [RelayCommand]
        private async Task SaveStockSettings()
        {
            ErrorMessage = string.Empty;
            SuccessMessage = string.Empty;

            if (!int.TryParse(StockAlertFrequencyHoursInput, out var freq) || freq < 1)
            {
                ErrorMessage = "La frecuencia debe ser un número mayor o igual a 1";
                return;
            }

            IsBusy = true;
            var result = await _settingsService.UpdateStockSettingsAsync(StockAlertEnabled, freq, StockAlertStartTime, StockAlertEndTime);
            IsBusy = false;

            if (!result.Success)
            {
                ErrorMessage = result.ErrorMessage ?? "No se pudo guardar la configuración";
                return;
            }
            SuccessMessage = "Configuración de stock actualizada";
        }

        // RF-083
        [RelayCommand]
        private async Task SaveReporteZSchedule()
        {
            ErrorMessage = string.Empty;
            SuccessMessage = string.Empty;

            IsBusy = true;
            var result = await _settingsService.UpdateReporteZScheduleAsync(ReporteZHour);
            IsBusy = false;

            if (!result.Success)
            {
                ErrorMessage = result.ErrorMessage ?? "No se pudo guardar el horario";
                return;
            }
            SuccessMessage = "Horario del Reporte Z actualizado";
        }

        // RF-084
        [RelayCommand]
        private async Task CreateBackup()
        {
            ErrorMessage = string.Empty;
            SuccessMessage = string.Empty;

            var confirm = System.Windows.MessageBox.Show(
                "¿Confirmas generar una copia de seguridad ahora?",
                "Confirmar respaldo", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);

            if (confirm != System.Windows.MessageBoxResult.Yes) return;

            IsBusy = true;
            var result = await _settingsService.CreateBackupAsync();
            IsBusy = false;

            if (!result.Success || result.Data == null)
            {
                ErrorMessage = result.ErrorMessage ?? "No se pudo generar el respaldo";
                return;
            }

            SuccessMessage = $"Respaldo generado: {result.Data.FileName} ({result.Data.SizeBytes / 1024} KB)";
            LastBackupAt = result.Data.CreatedAt;
        }
    }
}