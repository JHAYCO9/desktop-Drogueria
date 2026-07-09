using System.Windows.Controls;

namespace DrogueriaMickey.Desktop.Views.Auth
{
    public partial class LoginView : UserControl
    {
        public LoginView()
        {
            InitializeComponent();
        }

        private void PasswordBox_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is ViewModels.Auth.LoginViewModel vm)
            {
                vm.Password = PasswordBox.Password;
            }
        }
    }
}