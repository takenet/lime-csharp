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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Lime.Client.TestConsole
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void IsDarkMode_Checked(object sender, RoutedEventArgs e)
        {
            MenuItem isDarkMode = (MenuItem)sender;

            if (isDarkMode.IsChecked)
            {
                SessionView.Style = (Style)Resources["darkMode"];
                this.Style = (Style)Resources["darkMode"];

            }

            else
            {
                SessionView.Style = null;
                this.Style = null;
            }
        }
    }
}
