using System.Windows.Controls;
using System.Windows.Input;

namespace DrogueriaMickey.Desktop.Views.Dashboard
{
    public partial class DashboardView : UserControl
    {
        public DashboardView()
        {
            InitializeComponent();
        }

        private void OnAlertCardClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is ViewModels.Dashboard.DashboardViewModel vm && vm.GoToAlertsCommand.CanExecute(null))
                vm.GoToAlertsCommand.Execute(null);
        }

        private void OnCashCardClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is ViewModels.Dashboard.DashboardViewModel vm && vm.GoToCashRegisterCommand.CanExecute(null))
                vm.GoToCashRegisterCommand.Execute(null);
        }
    }
}