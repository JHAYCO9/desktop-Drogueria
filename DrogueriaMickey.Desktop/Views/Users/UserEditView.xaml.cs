using System.Windows.Controls;

namespace DrogueriaMickey.Desktop.Views.Users
{
    public partial class UserEditView : UserControl
    {
        public UserEditView() { InitializeComponent(); }

        private void TempPasswordBox_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is ViewModels.Users.UserEditViewModel vm)
                vm.TemporaryPassword = TempPasswordBox.Password;
        }
    }
}