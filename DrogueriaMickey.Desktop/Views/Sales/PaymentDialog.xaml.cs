using System.Windows;

namespace DrogueriaMickey.Desktop.Views.Sales
{
    public partial class PaymentDialog : Window
    {
        public PaymentDialog(ViewModels.Sales.PaymentDialogViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            viewModel.RequestClose += () => Close();
        }
    }
}