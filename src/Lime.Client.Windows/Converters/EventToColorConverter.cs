using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;
using Lime.Protocol;
using Lime.Messaging.Resources;

namespace Lime.Client.Windows.Converters
{
    public class EventToColorConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is Event?)
            {
                var notificationEvent = (Event?)value;

                switch (notificationEvent)
                {
                    case Event.Failed:
                        return Colors.Red;
                    case Event.Accepted:
                        return Colors.LightGreen;
                    case Event.Received:
                        return Colors.YellowGreen;
                    default:
                        break;
                }
            }
                        
            return Colors.LightGray;
            
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
