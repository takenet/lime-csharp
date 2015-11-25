using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lime.Client.TestConsole.ViewModels;
using Lime.Protocol;

namespace Lime.Client.TestConsole.Macros
{
    [Macro(Name = "Set local node", Category = "Session", IsActiveByDefault = true)]
    public class SetLocalNodeMacro : IMacro
    {
        private static string LOCAL_NODE_VARIABLE = "localNode";

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
                    .FirstOrDefault(v => v.Name.Equals(LOCAL_NODE_VARIABLE));

                if (sessionIdVariableViewModel == null)
                {
                    sessionIdVariableViewModel = new VariableViewModel()
                    {
                        Name = LOCAL_NODE_VARIABLE
                    };

                    sessionViewModel.Variables.Add(sessionIdVariableViewModel);
                }

                sessionIdVariableViewModel.Value = session.To.ToString();
            }

            return Task.FromResult<object>(null);
        }
    }
}
