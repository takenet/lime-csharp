using Lime.Protocol.Security;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lime.Protocol.Server
{
    public static class ServerChannelExtensions
    {
        public static async Task<Session> EstablishSessionAsync(this IServerChannel channel, IEnumerable<SessionCompression> enabledCompressionOptions, IEnumerable<SessionEncryption> enabledEncryptionOptions, 
            Func<Identity, Authentication, Node> authenticateFunc, CancellationToken cancellationToken)
        {
            var receivedSession = await channel.ReceiveNewSessionAsync(cancellationToken).ConfigureAwait(false);
            if (receivedSession.State == SessionState.New)
            {
                receivedSession = await channel.AuthenticateSessionAsync(
                    new[] { AuthenticationScheme.Plain },
                    cancellationToken).ConfigureAwait(false);

                var compressionOptions = enabledCompressionOptions.Intersect(channel.Transport.GetSupportedCompression()).ToArray();
                var encryptionOptions = enabledEncryptionOptions.Intersect(channel.Transport.GetSupportedEncryption()).ToArray();
                
                if (compressionOptions.Length > 1 ||
                    encryptionOptions.Length > 1)
                {
                    receivedSession = await channel.NegotiateSessionAsync(
                        compressionOptions,
                        encryptionOptions,
                        cancellationToken).ConfigureAwait(false);

                    if (receivedSession.State == SessionState.Negotiating &&
                        receivedSession.Compression.HasValue &&
                        compressionOptions.Contains(receivedSession.Compression.Value) &&
                        receivedSession.Encryption.HasValue &&
                        encryptionOptions.Contains(receivedSession.Encryption.Value))
                    {
                        await channel.SendNegotiatingSessionAsync(
                            receivedSession.Compression.Value,
                            receivedSession.Encryption.Value);

                        if (channel.Transport.Compression != receivedSession.Compression.Value)
                        {
                            await channel.Transport.SetCompressionAsync(
                                receivedSession.Compression.Value,
                                cancellationToken);
                        }

                        if (channel.Transport.Encryption != receivedSession.Encryption.Value)
                        {
                            await channel.Transport.SetEncryptionAsync(
                                receivedSession.Encryption.Value,
                                cancellationToken);
                        }
                    }
                }
                else if (compressionOptions.Length == 0 ||
                         !compressionOptions[0].Equals(channel.Transport.Compression))
                {
                    throw new ArgumentException("The transport doesn't support the specified compression options", "enabledCompressionOptions");
                }
                else if (encryptionOptions.Length == 0 ||
                         !encryptionOptions[0].Equals(channel.Transport.Encryption))
                {
                    throw new ArgumentException("The transport doesn't support the specified encryption options", "enabledEncryptionOptions");
                }
                                   
                if (receivedSession.State == SessionState.Authenticating)
                {
                    var plainAuthentication = receivedSession.Authentication as PlainAuthentication;
                    if (plainAuthentication != null)
                    {

                    }

                }

            }

            if (receivedSession.State != SessionState.Established)
            {

            }

            return receivedSession;
        }
    }
}
