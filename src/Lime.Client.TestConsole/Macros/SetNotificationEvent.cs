using Lime.Client.TestConsole.ViewModels;
using Lime.Protocol;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lime.Client.TestConsole.Macros
{
    [Macro(Name = "Set notification event", Category = "Notification", IsActiveByDefault = true)]
    public class SetNotificationEvent : IMacro
    {
        #region IMacro Members

        public Task ProcessAsync(EnvelopeViewModel envelopeViewModel, SessionViewModel sessionViewModel)
        {
            if (envelopeViewModel == null)
            {
                throw new ArgumentNullException("envelopeViewModel");
            }

            if (sessionViewModel == null)
            {
                throw new ArgumentNullException("sessionViewModel");
            }

            var notification = envelopeViewModel.Envelope as Notification;

            if (notification != null)
            {
                sessionViewModel.LastNotificationEvent = notification.Event;
            }

            return Task.FromResult<object>(null);
        }

        #endregion
    }
}