using System.Windows.Controls;
using System.Windows.Input;

namespace DrogueriaMickey.Desktop.Views.Sales
{
    public partial class SalePosView : UserControl
    {
        public SalePosView()
        {
            InitializeComponent();
            Loaded += (s, e) => BarcodeBox.Focus();
        }

        private void BarcodeBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && DataContext is ViewModels.Sales.SalePosViewModel vm)
            {
                if (vm.ScanAndAddCommand.CanExecute(null))
                    vm.ScanAndAddCommand.Execute(null);

                BarcodeBox.Focus();
            }
        }
    }
}