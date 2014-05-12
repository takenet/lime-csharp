using FirstFloor.ModernUI.Windows;
using FirstFloor.ModernUI.Windows.Navigation;
using Lime.Client.Windows.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Lime.Client.Windows.Pages
{
    /// <summary>
    /// Interaction logic for Login.xaml
    /// </summary>
    public partial class Login : UserControl
    {
        public Login()
        {
            InitializeComponent();
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter &&
                loginButton.Command != null &&
                loginButton.Command.CanExecute(null))
            {
                loginButton.Command.Execute(null);
            }
        }

        private void passwordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            // PasswordBox doesn't support data binding for security reasons
            var viewModel = this.DataContext as LoginViewModel;

            if (viewModel != null)
            {
                viewModel.Password = ((PasswordBox)sender).Password;
            }
        }
    }
}
