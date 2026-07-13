using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DrogueriaMickey.Desktop.Services;

namespace DrogueriaMickey.Desktop.ViewModels.Auth
{
    public partial class ChangePasswordViewModel : ObservableObject
    {
        private readonly UserService _userService;

        [ObservableProperty] private string currentPassword = string.Empty;
        [ObservableProperty] private string newPassword = string.Empty;
        [ObservableProperty] private string confirmPassword = string.Empty;
        [ObservableProperty] private string errorMessage = string.Empty;
        [ObservableProperty] private string successMessage = string.Empty;
        [ObservableProperty] private bool isBusy;

        public ChangePasswordViewModel(UserService userService)
        {
            _userService = userService;
        }

        [RelayCommand]
        private async Task Save()
        {
            ErrorMessage = string.Empty;
            SuccessMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(CurrentPassword) || string.IsNullOrWhiteSpace(NewPassword) || string.IsNullOrWhiteSpace(ConfirmPassword))
            {
                ErrorMessage = "Todos los campos son obligatorios";
                return;
            }

            if (NewPassword != ConfirmPassword)
            {
                ErrorMessage = "La nueva contraseña y su confirmación no coinciden";
                return;
            }

            IsBusy = true;
            var result = await _userService.ChangePasswordAsync(CurrentPassword, NewPassword, ConfirmPassword);
            IsBusy = false;

            if (!result.Success)
            {
                ErrorMessage = result.ErrorMessage ?? "No se pudo cambiar la contraseña";
                return;
            }

            SuccessMessage = "Contraseña actualizada correctamente";
            CurrentPassword = string.Empty;
            NewPassword = string.Empty;
            ConfirmPassword = string.Empty;
        }
    }
}