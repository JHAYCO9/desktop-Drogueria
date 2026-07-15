using System.Windows.Controls;
using System.Windows.Input;

namespace DrogueriaMickey.Desktop.Views.Inventory
{
    public partial class MedicineEditView : UserControl
    {
        public MedicineEditView()
        {
            InitializeComponent();
            Loaded += (s, e) => BarcodeBox.Focus();
        }
    }
}