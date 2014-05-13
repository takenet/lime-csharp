using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using Lime.Protocol.Resources;
using Lime.Client.Windows.Properties;

namespace Lime.Client.Windows.Converters
{
    public class PresenceToDescriptionConverter : IValueConverter
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
                        return Resources.Unavailable;
                    case PresenceStatus.Available:
                        return Resources.Available;
                    case PresenceStatus.Busy:
                        return Resources.Busy;
                    case PresenceStatus.Away:
                        return Resources.Away;
                    default:
                        return Resources.Unavailable;
                }
            }
            else
            {
                return Resources.Unavailable;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
