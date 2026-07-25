using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DrogueriaMickey.Desktop.Services;

namespace DrogueriaMickey.Desktop.ViewModels.Sales
{
    public partial class PaymentDialogViewModel : ObservableObject
    {
        private readonly SaleService _saleService;
        private readonly ReceiptPrintService _printService;
        private readonly CashDrawerService _drawerService;
        private readonly string _saleId;

        [ObservableProperty] private decimal totalAmount;
        [ObservableProperty] private string cashReceivedInput = string.Empty;
        [ObservableProperty] private string nequiAmountInput = string.Empty;
        [ObservableProperty] private decimal changeToGive;
        [ObservableProperty] private bool canConfirmCash;
        [ObservableProperty] private string errorMessage = string.Empty;
        [ObservableProperty] private bool isBusy;

        public bool PaymentCompleted { get; private set; }
        public string? ResultMessage { get; private set; }

        public event Action? RequestClose;

        public PaymentDialogViewModel(SaleService saleService, ReceiptPrintService printService, CashDrawerService drawerService, string saleId, decimal totalAmount)
        {
            _saleService = saleService;
            _printService = printService;
            _drawerService = drawerService;
            _saleId = saleId;
            TotalAmount = totalAmount;
        }

        partial void OnCashReceivedInputChanged(string value)
        {
            ErrorMessage = string.Empty;

            if (decimal.TryParse(value, out var received) && received >= 0)
            {
                ChangeToGive = received >= TotalAmount ? received - TotalAmount : 0;
                CanConfirmCash = received >= TotalAmount;
            }
            else
            {
                ChangeToGive = 0;
                CanConfirmCash = false;
            }
        }

        [RelayCommand]
        private async Task ConfirmCash()
        {
            ErrorMessage = string.Empty;

            if (!decimal.TryParse(CashReceivedInput, out var received) || received <= 0)
            {
                ErrorMessage = "Ingresa un valor recibido válido";
                return;
            }

            if (received < TotalAmount)
            {
                ErrorMessage = "El valor recibido es insuficiente para cubrir el total";
                return;
            }

            IsBusy = true;
            var result = await _saleService.FinalizeCashAsync(_saleId, received);

            if (!result.Success || result.Data == null)
            {
                IsBusy = false;
                ErrorMessage = result.ErrorMessage ?? "No se pudo finalizar la venta";
                return;
            }

            PaymentCompleted = true;
            ResultMessage = $"Venta #{result.Data.SaleNumber} finalizada.\nCambio a entregar: ${result.Data.ChangeGiven:N0}";

            await PrintAutomaticallyAsync(isCashPayment: true);

            IsBusy = false;
            RequestClose?.Invoke();
        }

        [RelayCommand]
        private async Task ConfirmNequi()
        {
            ErrorMessage = string.Empty;

            if (!decimal.TryParse(NequiAmountInput, out var amount) || amount <= 0)
            {
                ErrorMessage = "Ingresa un valor pagado válido";
                return;
            }

            if (amount < TotalAmount)
            {
                ErrorMessage = $"El valor pagado (${amount:N0}) es menor al total (${TotalAmount:N0})";
                return;
            }

            var confirm = System.Windows.MessageBox.Show(
                $"¿Confirmas la venta por ${TotalAmount:N0} pagada mediante Nequi?",
                "Confirmar pago Nequi", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);

            if (confirm != System.Windows.MessageBoxResult.Yes) return;

            IsBusy = true;
            var result = await _saleService.FinalizeNequiAsync(_saleId, amount);

            if (!result.Success)
            {
                IsBusy = false;
                ErrorMessage = result.ErrorMessage ?? "No se pudo finalizar la venta";
                return;
            }

            PaymentCompleted = true;
            ResultMessage = "Venta finalizada correctamente mediante Nequi";

            await PrintAutomaticallyAsync();

            IsBusy = false;
            RequestClose?.Invoke();
        }

        // RF-043: imprime el comprobante automáticamente, sin diálogo ni
        // clic extra, apenas la venta queda finalizada. Si la impresora
        // falla (apagada, sin papel, etc.), no bloquea el cierre de la
        // venta — solo se informa en el mensaje final.
        private async Task PrintAutomaticallyAsync(bool isCashPayment = false)
        {
            var receiptResult = await _saleService.GetReceiptAsync(_saleId);
            if (!receiptResult.Success || receiptResult.Data == null)
            {
                ResultMessage += "\n(No se pudo generar el comprobante para imprimir)";
                return;
            }

            try
            {
                _printService.Print(receiptResult.Data);
            }
            catch (Exception ex)
            {
                ResultMessage += $"\n(No se pudo imprimir: {ex.Message})";
            }

            if (isCashPayment)
            {
                try
                {
                    _drawerService.OpenDrawer();
                }
                catch (Exception ex)
                {
                    ResultMessage += $"\n(No se pudo abrir la caja: {ex.Message})";
                }
            }
        }

        [RelayCommand]
        private void CancelDialog()
        {
            PaymentCompleted = false;
            RequestClose?.Invoke();
        }
    }
}