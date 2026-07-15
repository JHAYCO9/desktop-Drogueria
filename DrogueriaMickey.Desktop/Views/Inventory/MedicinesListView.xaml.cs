using System.Windows.Controls;
using System.Windows.Input;

namespace DrogueriaMickey.Desktop.Views.Inventory
{
    public partial class MedicinesListView : UserControl
    {
        public MedicinesListView()
        {
            InitializeComponent();
            Loaded += (s, e) => BarcodeBox.Focus(); // el foco queda listo para escanear apenas abres la vista
        }

        private void BarcodeBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && DataContext is ViewModels.Inventory.MedicinesListViewModel vm)
            {
                if (vm.ScanBarcodeCommand.CanExecute(null))
                    vm.ScanBarcodeCommand.Execute(null);

                BarcodeBox.Focus(); // vuelve a dejar el foco listo para el siguiente escaneo
            }
        }
    }
}