using Lime.Client.TestConsole.ViewModels;
using Lime.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Client.TestConsole.Macros
{
    [Macro(Name = "Close transport on finished / failed", Category = "Session", IsActiveByDefault = true)]
    public class CloseTransportMacro : IMacro
    {
        #region IMacro Members

        public async Task ProcessAsync(EnvelopeViewModel envelopeViewModel, SessionViewModel sessionViewModel)
        {
            if (envelopeViewModel == null)
            {
                throw new ArgumentNullException("envelopeViewModel");
            }

            if (sessionViewModel == null)
            {
                throw new ArgumentNullException("sessionViewModel");
            }

            var session = envelopeViewModel.Envelope as Session;

            if (session != null &&
                new[] { SessionState.Finished, SessionState.Failed }.Contains(session.State) &&
                sessionViewModel.CloseTransportCommand.CanExecute(null))
            {
                await sessionViewModel.CloseTransportCommand.ExecuteAsync(null);
            }
        }

        #endregion
    }
}