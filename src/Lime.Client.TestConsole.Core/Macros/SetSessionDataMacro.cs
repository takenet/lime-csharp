using Lime.Client.TestConsole.ViewModels;
using Lime.Protocol;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lime.Client.TestConsole.Macros
{
    [Macro(Name = "Set session data", Category = "Session", IsActiveByDefault = true)]
    public class SetSessionDataMacro : IMacro
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

            var session = envelopeViewModel.Envelope as Session;

            if (session != null)
            {
                sessionViewModel.LastSessionState = session.State;

                if (session.State == SessionState.Established)
                {
                    sessionViewModel.LocalNode = session.To;
                    sessionViewModel.RemoteNode = session.From;
                }
            }

            return Task.FromResult<object>(null);
        }

        #endregion
    }
}