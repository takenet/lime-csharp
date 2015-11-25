using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lime.Client.TestConsole.ViewModels;
using Lime.Protocol;

namespace Lime.Client.TestConsole.Macros
{
    [Macro(Name = "Clean guest identity", Category = "Session", IsActiveByDefault = true)]
    public class CleanGuestIdentityMacro : IMacro
    {
        private static string GUEST_IDENTITY_VARIABLE = "guestIdentity";

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

            if (session != null &&
                session.Id != Guid.Empty &&
                session.State == SessionState.Established)
            {
                var sessionIdVariableViewModel = sessionViewModel
                    .Variables
                    .FirstOrDefault(v => v.Name.Equals(GUEST_IDENTITY_VARIABLE));

                if (sessionIdVariableViewModel != null)
                {
                    sessionViewModel.Variables.Remove(sessionIdVariableViewModel);
                }
            }

            return Task.FromResult<object>(null);
        }
    }
}
