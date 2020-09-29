using Lime.Protocol.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Lime.Client.TestConsole.Converters
{
    public class IsRawToBrushConverter : IMultiValueConverter
    {
        #region IValueConverter Members

        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var isRaw = values[0];
            var darkMode = values[1];

            if (isRaw is bool && (bool)isRaw)
            {
                return new SolidColorBrush(Colors.Red);
            }

            if (darkMode is Style)
            {
                return new SolidColorBrush(Colors.White);
            }

            return new SolidColorBrush(Colors.Black);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
