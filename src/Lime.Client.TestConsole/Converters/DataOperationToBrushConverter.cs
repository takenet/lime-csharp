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
    public class DataOperationToBrushConverter : IMultiValueConverter
    {

        private readonly SolidColorBrush LightDarkMode = (SolidColorBrush)(new BrushConverter().ConvertFrom("#424242"));
        private readonly SolidColorBrush NormalDarkMode = (SolidColorBrush)(new BrushConverter().ConvertFrom("#212121"));

        #region IValueConverter Members

        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            //First value is Direction property
            //Second value is IsDarkMode property

            if (values[0] is DataOperation && ((DataOperation)values[0]) == DataOperation.Receive)
            {
                if (values[1] is Style)
                {
                    return NormalDarkMode;
                }

                return new SolidColorBrush(Colors.LightGray);
            }

            if (values[1] is Style)
            {
                return LightDarkMode;
            }

            return new SolidColorBrush(Colors.White);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
