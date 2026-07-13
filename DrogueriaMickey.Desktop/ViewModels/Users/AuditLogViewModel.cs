using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DrogueriaMickey.Desktop.Services;

namespace DrogueriaMickey.Desktop.ViewModels.Users
{
    public partial class AuditLogViewModel : ObservableObject
    {
        private readonly UserService _userService;

        [ObservableProperty] private ObservableCollection<AuditLogDto> logs = new();
        [ObservableProperty] private string errorMessage = string.Empty;
        [ObservableProperty] private bool isBusy;

        public AuditLogViewModel(UserService userService)
        {
            _userService = userService;
            _ = Load();
        }

        [RelayCommand]
        private async Task Load()
        {
            IsBusy = true;
            var result = await _userService.GetAuditLogsAsync();
            IsBusy = false;

            if (!result.Success)
            {
                ErrorMessage = result.ErrorMessage ?? "No se pudo cargar la auditoría";
                return;
            }

            Logs = new ObservableCollection<AuditLogDto>(result.Data ?? new());
        }
    }
}