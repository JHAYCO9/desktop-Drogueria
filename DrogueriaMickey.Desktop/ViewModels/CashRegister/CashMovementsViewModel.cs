using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DrogueriaMickey.Desktop.Services;

namespace DrogueriaMickey.Desktop.ViewModels.CashRegister
{
    public partial class CashMovementsViewModel : ObservableObject
    {
        private readonly CashRegisterService _cashService;

        [ObservableProperty] private ObservableCollection<CashMovementDto> movements = new();
        [ObservableProperty] private string fromDate = string.Empty;
        [ObservableProperty] private string toDate = string.Empty;
        [ObservableProperty] private string errorMessage = string.Empty;
        [ObservableProperty] private bool isBusy;

        public CashMovementsViewModel(CashRegisterService cashService)
        {
            _cashService = cashService;
            _ = LoadAll();
        }

        [RelayCommand]
        private async Task LoadAll()
        {
            ErrorMessage = string.Empty;
            IsBusy = true;
            var result = await _cashService.GetMovementsAsync();
            IsBusy = false;

            if (!result.Success)
            {
                ErrorMessage = result.ErrorMessage ?? "No se pudieron cargar los movimientos";
                return;
            }

            Movements = new ObservableCollection<CashMovementDto>(result.Data ?? new());
        }

        [RelayCommand]
        private async Task FilterByDate()
        {
            ErrorMessage = string.Empty;
            IsBusy = true;
            var result = await _cashService.GetMovementsAsync(from: FromDate, to: ToDate);
            IsBusy = false;

            if (!result.Success)
            {
                ErrorMessage = result.ErrorMessage ?? "No se pudieron cargar los movimientos";
                return;
            }

            Movements = new ObservableCollection<CashMovementDto>(result.Data ?? new());
        }
    }
}