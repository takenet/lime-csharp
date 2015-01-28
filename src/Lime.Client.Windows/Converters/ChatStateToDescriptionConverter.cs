using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using Lime.Messaging.Contents;
using Lime.Messaging.Resources;

namespace Lime.Client.Windows.Converters
{
    public class ChatStateToDescriptionConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is ChatStateEvent)
            {
                var chatState = (ChatStateEvent)value;

                switch (chatState)
                {
                    case ChatStateEvent.Starting:
                        return "Iniciou a conversa";
                    case ChatStateEvent.Composing:
                        return "Digitando...";
                    case ChatStateEvent.Paused:
                        return null;
                    case ChatStateEvent.Deleting:
                        return "Apagando...";
                    case ChatStateEvent.Gone:
                        return "Saiu da conversa";
                    default:
                        return null;
                }
            }
            else
            {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
