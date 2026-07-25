using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DrogueriaMickey.Desktop.Services;

namespace DrogueriaMickey.Desktop.ViewModels.Auth
{
    public partial class LoginViewModel : ObservableObject
    {
        private readonly AuthService _authService;

        [ObservableProperty]
        private string username = string.Empty;

        [ObservableProperty]
        private string password = string.Empty;

        [ObservableProperty]
        private string errorMessage = string.Empty;

        [ObservableProperty]
        private bool isBusy;

        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
        public bool IsNotBusy => !IsBusy;

        public event Action? LoginSucceeded;

        public LoginViewModel(AuthService authService)
        {
            _authService = authService;
        }

        [RelayCommand]
        private async Task Login()
        {
            ErrorMessage = string.Empty;
            OnPropertyChanged(nameof(HasError));

            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Usuario y contraseña son obligatorios";
                OnPropertyChanged(nameof(HasError));
                return;
            }

            IsBusy = true;
            OnPropertyChanged(nameof(IsNotBusy));

            try
            {
                var result = await _authService.LoginAsync(Username, Password);

                if (!result.Success)
                {
                    ErrorMessage = result.ErrorMessage ?? "Usuario o contraseña incorrectos";
                    OnPropertyChanged(nameof(HasError));
                    return;
                }

                if (result.WasOffline)
                {
                    ErrorMessage = "Conectado en modo sin conexión — los cambios se sincronizarán cuando vuelva la red";
                    OnPropertyChanged(nameof(HasError));
                }

                LoginSucceeded?.Invoke();
            }
            catch (Exception ex)
            {
                // Con esto, si algo similar vuelve a pasar, al menos verás el
                // error real en vez de que la app se quede "colgada" en silencio.
                ErrorMessage = $"Error inesperado: {ex.Message}";
                OnPropertyChanged(nameof(HasError));
            }
            finally
            {
                IsBusy = false;
                OnPropertyChanged(nameof(IsNotBusy));
            }
        }
    }
}