using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DrogueriaMickey.Desktop.Services;

namespace DrogueriaMickey.Desktop.ViewModels.Returns
{
    public partial class ReturnsHistoryViewModel : ObservableObject
    {
        private readonly ReturnService _returnService;

        [ObservableProperty] private ObservableCollection<ReturnDto> returns = new();
        [ObservableProperty] private string errorMessage = string.Empty;
        [ObservableProperty] private bool isBusy;

        public ReturnsHistoryViewModel(ReturnService returnService)
        {
            _returnService = returnService;
            _ = Load();
        }

        [RelayCommand]
        private async Task Load()
        {
            ErrorMessage = string.Empty;
            IsBusy = true;
            var result = await _returnService.ListReturnsAsync();
            IsBusy = false;

            if (!result.Success)
            {
                ErrorMessage = result.ErrorMessage ?? "No se pudieron cargar las devoluciones";
                return;
            }

            Returns = new ObservableCollection<ReturnDto>(result.Data ?? new());
        }
    }
}