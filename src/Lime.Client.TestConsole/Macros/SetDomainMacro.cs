using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lime.Client.TestConsole.ViewModels;
using Lime.Protocol;

namespace Lime.Client.TestConsole.Macros
{
    [Macro(Name = "Set domain", Category = "Session", IsActiveByDefault = true)]
    public class SetDomainMacro : IMacro
    {
        private static string DOMAIN_VARIABLE = "domain";

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
                session.Id != null &&
                session.State == SessionState.Established)
            {
                var sessionIdVariableViewModel = sessionViewModel
                    .Variables
                    .FirstOrDefault(v => v.Name.Equals(DOMAIN_VARIABLE));

                if (sessionIdVariableViewModel == null)
                {
                    sessionIdVariableViewModel = new VariableViewModel()
                    {
                        Name = DOMAIN_VARIABLE
                    };

                    sessionViewModel.Variables.Add(sessionIdVariableViewModel);
                }

                sessionIdVariableViewModel.Value = session.From.Domain;
            }

            return Task.FromResult<object>(null);
        }
    }
}
