using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace LGC.PTI.Plugin.Client.ConfigPages.Plugin.General
{
    /// <summary>
    /// Interaction logic for SampleConfigPageView.xaml
    /// </summary>
    public partial class ConfigPageView : UserControl
    {
        public ConfigPageView()
        {
            InitializeComponent();
        }

        //private void OnPasswordChanged(object sender, RoutedEventArgs e)
        //{
        //    if (DataContext is ConfigPage vm && sender is PasswordBox passwordBox)
        //    {
        //        vm.SCLoginPassword = passwordBox.Password;
        //    }
        //}

        private void LGCClientIdChange(object sender, RoutedEventArgs e)
        {
            if (DataContext is ConfigPage vm && sender is PasswordBox passwordBox)
            {
                vm.ClientId = passwordBox.Password;
            }
        }

        private void LGCClientSecretChange(object sender, RoutedEventArgs e)
        {
            if (DataContext is ConfigPage vm && sender is PasswordBox passwordBox)
            {
                vm.ClientSecret = passwordBox.Password;
            }
        }

        private void OnlyNumbersFromInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !int.TryParse(e.Text, out _);
        }

        private void OnlyNumbersFromPaste(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(DataFormats.Text))
            {
                string text = (string)e.DataObject.GetData(DataFormats.Text);
                if (!int.TryParse(text, out _))
                {
                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }
    }
}