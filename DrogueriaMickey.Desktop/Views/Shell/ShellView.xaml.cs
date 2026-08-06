using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace DrogueriaMickey.Desktop.Views.Shell
{
    public partial class ShellView : UserControl
    {
        public ShellView()
        {
            InitializeComponent();
            if (DataContext is ViewModels.MainViewModel vm)
            {
                vm.Bot.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(vm.Bot.IsOpen))
                        AnimateChatPanel(vm.Bot.IsOpen);
                };
            }
            else
            {
                DataContextChanged += (s, e) =>
                {
                    if (DataContext is ViewModels.MainViewModel mvm)
                    {
                        mvm.Bot.PropertyChanged += (s2, e2) =>
                        {
                            if (e2.PropertyName == nameof(mvm.Bot.IsOpen))
                                AnimateChatPanel(mvm.Bot.IsOpen);
                        };
                    }
                };
            }
        }

        private void AnimateChatPanel(bool isOpen)
        {
            var animation = new DoubleAnimation
            {
                To = isOpen ? 0 : 360,
                Duration = TimeSpan.FromMilliseconds(250),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            ChatPanelTransform.BeginAnimation(TranslateTransform.XProperty, animation);
        }

        private void BotInputBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && DataContext is ViewModels.MainViewModel vm)
            {
                if (vm.Bot.SendCommand.CanExecute(null))
                    vm.Bot.SendCommand.Execute(null);
            }
        }
    }
}