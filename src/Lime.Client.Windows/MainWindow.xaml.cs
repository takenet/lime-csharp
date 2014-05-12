using FirstFloor.ModernUI.Windows.Controls;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
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

namespace Lime.Client.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : ModernWindow
    {
        private static Dictionary<string, Type> _windowTypeNameDictionary;
        
        #region Constructor

        static MainWindow()
        {
            _windowTypeNameDictionary = new Dictionary<string, Type>();

            var windowTypes = Assembly
                .GetExecutingAssembly()
                .GetTypes()
                .Where(t => typeof(Window).IsAssignableFrom(t));


            foreach (var windowType in windowTypes)
            {
                if (windowType == typeof(MainWindow))
                {
                    continue;
                }

                _windowTypeNameDictionary.Add(windowType.Name, windowType);
            }
        }

        public MainWindow()
        {
            InitializeComponent();

            Messenger.Default.Register<OpenWindowMessage>(
                this,
                OpenWindow
                );
        }

        #endregion


        private void OpenWindow(OpenWindowMessage message)
        {
            if (message == null)
            {
                throw new ArgumentNullException("message");
            }

            try
            {
                var window = base.OwnedWindows
                    .Cast<Window>()
                    .Where(w => w.DataContext == message.DataContext)
                    .FirstOrDefault();

                if (window == null)
                {
                    string windowName = message.WindowName;
                    if (!windowName.EndsWith("Window"))
                    {
                        windowName = string.Format("{0}Window", message.WindowName);
                    }

                    if (_windowTypeNameDictionary.ContainsKey(windowName))
                    {
                        var windowType = _windowTypeNameDictionary[windowName];
                        window = (Window)Activator.CreateInstance(windowType);
                        window.DataContext = message.DataContext;
                        window.Owner = this;

                        window.Show();
                    }
                }
                else
                {
                    if (!window.IsVisible)
                    {
                        window.Show();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                throw;
            }
        }

        public ModernFrame ContentFrame
        {
            get { return (ModernFrame)base.GetTemplateChild("ContentFrame"); }
        }
    }
}
