using Lime.Client.TestConsole.ViewModels;
using Lime.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Client.TestConsole.Macros
{
    [Macro(Name = "Notify message consumed", Category = "Notification", IsActiveByDefault = false)]
    public class NotifyMessageConsumedMacro : IMacro
    {
        #region IMacro Members

        public async Task ProcessAsync(EnvelopeViewModel envelopeViewModel, SessionViewModel sessionViewModel)
        {
            if (envelopeViewModel == null)
            {
                throw new ArgumentNullException(nameof(envelopeViewModel));
            }

            if (sessionViewModel == null)
            {
                throw new ArgumentNullException(nameof(sessionViewModel));
            }

            var message = envelopeViewModel.Envelope as Message;

            if (message != null &&
                !message.Id.IsNullOrEmpty())
            {
                var notification = new Notification()
                {
                    Id = message.Id,
                    To = message.Pp ?? message.From,
                    Event = Event.Consumed
                };

                var notificationEnvelopeViewModel = new EnvelopeViewModel()
                {
                    Envelope = notification
                };

                sessionViewModel.InputJson = notificationEnvelopeViewModel.Json;
                
                if (sessionViewModel.SendCommand.CanExecute(null))
                {
                    await sessionViewModel.SendCommand.ExecuteAsync(null);
                }
            }
        }

        #endregion
    }
}
