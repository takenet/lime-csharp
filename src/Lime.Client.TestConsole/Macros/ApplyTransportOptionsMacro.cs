using Lime.Client.TestConsole.ViewModels;
using Lime.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Client.TestConsole.Macros
{
    [Macro(Name = "Apply transport negotiation options", Category = "Session", IsActiveByDefault = true)]
    public class ApplyTransportOptionsMacro : IMacro
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
            var transport = sessionViewModel.Transport;

            if (session != null &&
                session.State == SessionState.Negotiating &&
                session.Compression.HasValue &&
                session.Encryption.HasValue &&
                transport != null)
            {
                var cancellationToken = TimeSpan.FromSeconds(15).ToCancellationToken();

                if (transport.Compression != session.Compression.Value)
                {
                    await transport.SetCompressionAsync(session.Compression.Value, cancellationToken);
                }

                if (transport.Encryption != session.Encryption.Value)
                {
                    await transport.SetEncryptionAsync(session.Encryption.Value, cancellationToken);
                }
            }
        }

        #endregion
    }
}
