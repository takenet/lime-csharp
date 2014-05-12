using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;
using Lime.Protocol.Resources;

namespace Lime.Client.Windows.Converters
{
    public class PresenceToColorConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is PresenceStatus)
            {
                var presenceStatus = (PresenceStatus)value;

                switch (presenceStatus)
                {
                    case PresenceStatus.Unavailable:
                        return Colors.LightGray;
                    case PresenceStatus.Available:
                        return Colors.LightGreen;
                    case PresenceStatus.Busy:
                        return Colors.Salmon;
                    case PresenceStatus.Away:
                        return Colors.Yellow;
                    default:
                        return Colors.LightGray;
                }
            }
            else
            {
                return Colors.LightGray;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
