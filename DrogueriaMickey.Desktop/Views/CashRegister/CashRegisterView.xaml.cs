using System.Windows.Controls;

namespace DrogueriaMickey.Desktop.Views.CashRegister
{
    public partial class CashRegisterView : UserControl
    {
        public CashRegisterView() { InitializeComponent(); }

        private void IngresoRadio_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is ViewModels.CashRegister.CashRegisterViewModel vm)
                vm.ManualMovementType = "ingreso_manual";
        }

        private void EgresoRadio_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is ViewModels.CashRegister.CashRegisterViewModel vm)
                vm.ManualMovementType = "egreso_manual";
        }
    }
}