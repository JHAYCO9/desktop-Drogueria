using CommunityToolkit.Mvvm.ComponentModel;

namespace DrogueriaMickey.Desktop.Navigation
{
    public partial class NavigationService : ObservableObject
    {
        [ObservableProperty]
        private object? currentView;

        public void NavigateTo(object view)
        {
            CurrentView = view;
        }
    }
}