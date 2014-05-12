using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using Lime.Protocol.Resources;

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
                        return "Indisponível";
                    case PresenceStatus.Available:
                        return "Disponível";
                    case PresenceStatus.Busy:
                        return "Ocupado";
                    case PresenceStatus.Away:
                        return "Ausente";
                    default:
                        return "Indisponível";
                }
            }
            else
            {
                return "Indisponível";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
