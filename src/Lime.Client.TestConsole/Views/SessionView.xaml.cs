using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Lime.Client.TestConsole.Views
{
    /// <summary>
    /// Interaction logic for SessionView.xaml
    /// </summary>
    public partial class SessionView : UserControl
    {
        public SessionView()
        {
            InitializeComponent();
            _darModeStyle = (Style)Resources["darkMode"];
            _darkModeJsonInputStyle = (Style)Resources["jsonErrorStyleDarkMode"];
            _jsonInputStyle = (Style)Resources["jsonErrorStyle"];
        }

        private bool _autoScroll;
        private Style _darModeStyle;
        private Style _darkModeJsonInputStyle;
        private Style _jsonInputStyle;

        private void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            var scrollViewer = (ScrollViewer)sender;

            // User scroll event : set or unset autoscroll mode
            if (e.ExtentHeightChange == 0)
            {   // Content unchanged : user scroll event
                if (scrollViewer.VerticalOffset == scrollViewer.ScrollableHeight)
                {   // Scroll bar is in bottom
                    // Set autoscroll mode
                    _autoScroll = true;
                }
                else
                {   // Scroll bar isn't in bottom
                    // Unset autoscroll mode
                    _autoScroll = false;
                }
            }

            // Content scroll event : autoscroll eventually
            if (_autoScroll && e.ExtentHeightChange != 0)
            {   // Content changed and autoscroll mode set
                // Autoscroll
                scrollViewer.ScrollToVerticalOffset(scrollViewer.ExtentHeight);
            }
        }

        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            ScrollViewer scv = (ScrollViewer)sender;
            scv.ScrollToVerticalOffset(scv.VerticalOffset - e.Delta);
            e.Handled = true;
        }

        private void ScrollViewerContents_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            ScrollViewer scv = (ScrollViewer)sender;
            scv.ScrollToVerticalOffset(scv.VerticalOffset - e.Delta);
            ChangeFontSize(scv, e);
        }


        private void JsonInput_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            JsonInput.ScrollToVerticalOffset((JsonInput.VerticalOffset - e.Delta) / 2);
            ChangeFontSize((Control)sender, e);
        }

        private void ChangeFontSize(Control control, MouseWheelEventArgs e)
        {
            e.Handled = true;

            if (Keyboard.Modifiers != ModifierKeys.Control)
                return;

            if (e.Delta > 0)
                ++control.FontSize;
            else
                --control.FontSize;
        }

        private void IsDarkMode_Checked(object sender, RoutedEventArgs e)
        {
            var darkMode = (ToggleButton)sender;

            if (darkMode.IsChecked == true)
            {
                this.Style = _darModeStyle;
                EnvelopesListBox.Style = _darModeStyle;
                this.JsonInput.Style = _darkModeJsonInputStyle;
            }
            else
            {
                this.Style = null;
                EnvelopesListBox.Style = null;
                this.JsonInput.Style = _jsonInputStyle;
            }
        }

    }
}
