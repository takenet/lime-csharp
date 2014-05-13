using FirstFloor.ModernUI.Windows.Controls;
using GalaSoft.MvvmLight.Messaging;
using Lime.Client.Windows.Shared;
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

namespace Lime.Client.Windows
{
    /// <summary>
    /// Interaction logic for ConversationWindow.xaml
    /// </summary>
    public partial class ConversationWindow : ModernWindow
    {
        public ConversationWindow()
        {
            InitializeComponent();
            Messenger.Default.Register<FlashWindowMessage>(this, FlashWindow);
        }

        private void FlashWindow(FlashWindowMessage message)
        {
            if (message == null)
            {
                throw new ArgumentNullException("message");
            }

            if (message.DataContext == base.DataContext)
            {
                WindowsUtils.FlashWindow(this, message.Mode);
            }
        }
    }

    internal class FlashWindowMessage
    {
        #region Constructor

        public FlashWindowMessage(object dataContext, FlashMode mode)
        {
            this.DataContext = dataContext;
            this.Mode = mode;
        }

        #endregion

        public object DataContext { get; private set; }

        public FlashMode Mode { get; private set; }
    }
}
