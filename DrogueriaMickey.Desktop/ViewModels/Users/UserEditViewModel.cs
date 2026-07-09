using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DrogueriaMickey.Desktop.Services;
using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Reflection.Metadata;
using System.Threading.Tasks;
using System.Linq;

namespace DrogueriaMickey.Desktop.ViewModels.Users
{
    public partial class UserEditViewModel : ObservableObject
    {
        private readonly UserService _userService;
        private string? _editingUserId;

        [ObservableProperty] private string fullName = string.Empty;
        [ObservableProperty] private string documentId = string.Empty;
        [ObservableProperty] private string username = string.Empty;
        [ObservableProperty] private string temporaryPassword = string.Empty;
        [ObservableProperty] private RoleDto? selectedRole;
        [ObservableProperty] private bool isActive = true;
        [ObservableProperty] private string errorMessage = string.Empty;
        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private ObservableCollection<RoleDto> roles = new();

        public bool IsEditMode => _editingUserId != null;
        public bool IsCreateMode => !IsEditMode;
        public string Title => IsEditMode ? "Editar usuario" : "Nuevo usuario";

        public event Action? Saved;

        public UserEditViewModel(UserService userService)
        {
            _userService = userService;
            _ = LoadRoles();
        }

        public void Initialize(UserListItemDto? existing)
        {
            if (existing == null)
            {
                _editingUserId = null;
                FullName = string.Empty;
                Username = string.Empty;
                DocumentId = string.Empty;
                TemporaryPassword = string.Empty;
                IsActive = true;
            }
            else
            {
                _editingUserId = existing.Id;
                FullName = existing.FullName;
                Username = existing.Username;
                IsActive = existing.IsActive;
                // SelectedRole se ajusta una vez cargan los roles (ver LoadRoles)
                _pendingRoleId = existing.RoleId;
            }
            OnPropertyChanged(nameof(IsEditMode));
            OnPropertyChanged(nameof(IsCreateMode));
            OnPropertyChanged(nameof(Title));
        }

        private string? _pendingRoleId;

        private async Task LoadRoles()
        {
            var result = await _userService.ListRolesAsync();
            if (result.Success && result.Data != null)
            {
                Roles = new ObservableCollection<RoleDto>(result.Data);
                if (_pendingRoleId != null)
                {
                    SelectedRole = Roles.FirstOrDefault(r => r.Id == _pendingRoleId);
                }
            }
        }

        [RelayCommand]
        private async Task Save()
        {
            ErrorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(FullName) || string.IsNullOrWhiteSpace(Username) || SelectedRole == null)
            {
                ErrorMessage = "Nombre, usuario y rol son obligatorios";
                return;
            }

            IsBusy = true;

            if (IsCreateMode)
            {
                if (string.IsNullOrWhiteSpace(TemporaryPassword))
                {
                    ErrorMessage = "La contraseña temporal es obligatoria";
                    IsBusy = false;
                    return;
                }

                var result = await _userService.RegisterUserAsync(FullName, DocumentId, Username, TemporaryPassword, SelectedRole.Id);
                IsBusy = false;

                if (!result.Success)
                {
                    ErrorMessage = result.ErrorMessage ?? "No se pudo registrar el usuario";
                    return;
                }
            }
            else
            {
                var result = await _userService.UpdateUserAsync(_editingUserId!, FullName, Username, SelectedRole.Id, IsActive);
                IsBusy = false;

                if (!result.Success)
                {
                    ErrorMessage = result.ErrorMessage ?? "No se pudo actualizar el usuario";
                    return;
                }
            }

            Saved?.Invoke();
        }
    }
}