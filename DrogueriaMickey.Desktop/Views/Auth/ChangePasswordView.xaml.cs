using System.Windows.Controls;

namespace DrogueriaMickey.Desktop.Views.Auth
{
    public partial class ChangePasswordView : UserControl
    {
        public ChangePasswordView() { InitializeComponent(); }

        private void CurrentBox_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is ViewModels.Auth.ChangePasswordViewModel vm) vm.CurrentPassword = CurrentBox.Password;
        }
        private void NewBox_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is ViewModels.Auth.ChangePasswordViewModel vm) vm.NewPassword = NewBox.Password;
        }
        private void ConfirmBox_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is ViewModels.Auth.ChangePasswordViewModel vm) vm.ConfirmPassword = ConfirmBox.Password;
        }
    }
}