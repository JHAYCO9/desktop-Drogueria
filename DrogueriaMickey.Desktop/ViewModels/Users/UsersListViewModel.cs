using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DrogueriaMickey.Desktop.Navigation;
using DrogueriaMickey.Desktop.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;

namespace DrogueriaMickey.Desktop.ViewModels.Users
{
    public partial class UsersListViewModel : ObservableObject
    {
        private readonly UserService _userService;
        private readonly IServiceProvider _services;
        private readonly NavigationService _navigation;

        [ObservableProperty]
        private ObservableCollection<UserListItemDto> users = new();

        [ObservableProperty]
        private string errorMessage = string.Empty;

        [ObservableProperty]
        private bool isBusy;

        public UsersListViewModel(UserService userService, IServiceProvider services, NavigationService navigation)
        {
            _userService = userService;
            _services = services;
            _navigation = navigation;
            _ = LoadUsers();
        }

        [RelayCommand]
        private async Task LoadUsers()
        {
            IsBusy = true;
            var result = await _userService.ListUsersAsync();
            IsBusy = false;

            if (!result.Success)
            {
                ErrorMessage = result.ErrorMessage ?? "No se pudieron cargar los usuarios";
                return;
            }

            Users = new ObservableCollection<UserListItemDto>(result.Data ?? new());
        }

        [RelayCommand]
        private void NewUser()
        {
            var vm = _services.GetRequiredService<UserEditViewModel>();
            vm.Initialize(null); // null = modo creación
            vm.Saved += async () => await LoadUsers();
            var view = new Views.Users.UserEditView { DataContext = vm };
            _navigation.NavigateTo(view);
        }

        [RelayCommand]
        private void EditUser(UserListItemDto user)
        {
            var vm = _services.GetRequiredService<UserEditViewModel>();
            vm.Initialize(user);
            vm.Saved += async () => await LoadUsers();
            var view = new Views.Users.UserEditView { DataContext = vm };
            _navigation.NavigateTo(view);
        }
    }
}