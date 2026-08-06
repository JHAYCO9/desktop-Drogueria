using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DrogueriaMickey.Desktop.Services;

namespace DrogueriaMickey.Desktop.ViewModels.Returns
{
    public partial class ReturnsHistoryViewModel : ObservableObject
    {
        private readonly ReturnService _returnService;
        private readonly SessionService _session;

        [ObservableProperty] private ObservableCollection<ReturnDto> returns = new();
        [ObservableProperty] private string errorMessage = string.Empty;
        [ObservableProperty] private bool isBusy;

        public bool IsAdmin => _session.IsAdministrator;

        public ReturnsHistoryViewModel(ReturnService returnService, SessionService session)
        {
            _returnService = returnService;
            _session = session;
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

        // RF-094
        [RelayCommand]
        private async Task VoidReturn(ReturnDto ret)
        {
            if (ret.IsVoided) return;

            var input = Microsoft.VisualBasic.Interaction.InputBox(
                "Motivo de la anulación (mínimo 3 caracteres):", "Anular devolución", "");

            if (string.IsNullOrWhiteSpace(input) || input.Length < 3) return;

            var confirm = System.Windows.MessageBox.Show(
                $"¿Confirmas anular esta devolución? Se revertirá el inventario.",
                "Confirmar anulación", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning);

            if (confirm != System.Windows.MessageBoxResult.Yes) return;

            IsBusy = true;
            var result = await _returnService.VoidReturnAsync(ret.Id, input);
            IsBusy = false;

            if (!result.Success)
            {
                ErrorMessage = result.ErrorMessage ?? "No se pudo anular la devolución";
                return;
            }

            await Load();
        }
    }
}