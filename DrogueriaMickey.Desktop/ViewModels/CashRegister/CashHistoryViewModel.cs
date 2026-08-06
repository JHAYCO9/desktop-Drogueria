using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DrogueriaMickey.Desktop.Services;

namespace DrogueriaMickey.Desktop.ViewModels.CashRegister
{
    public partial class CashHistoryViewModel : ObservableObject
    {
        private readonly CashRegisterService _cashService;

        [ObservableProperty] private ObservableCollection<ClosedRegisterDto> closedRegisters = new();
        [ObservableProperty] private string fromDate = string.Empty;
        [ObservableProperty] private string toDate = string.Empty;
        [ObservableProperty] private string errorMessage = string.Empty;
        [ObservableProperty] private bool isBusy;

        public CashHistoryViewModel(CashRegisterService cashService)
        {
            _cashService = cashService;
            _ = Load();
        }

        [RelayCommand]
        private async Task Load()
        {
            ErrorMessage = string.Empty;
            IsBusy = true;
            var result = await _cashService.GetHistoryAsync(
                string.IsNullOrWhiteSpace(FromDate) ? null : FromDate,
                string.IsNullOrWhiteSpace(ToDate) ? null : ToDate);
            IsBusy = false;

            if (!result.Success)
            {
                ErrorMessage = result.ErrorMessage ?? "No se pudo cargar el historial de cierres";
                return;
            }

            ClosedRegisters = new ObservableCollection<ClosedRegisterDto>(result.Data ?? new());
        }
    }
}