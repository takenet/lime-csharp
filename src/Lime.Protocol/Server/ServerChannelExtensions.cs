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
        /// <summary>
        /// Establishes a server channel with transport options negotiation and authentication.
        /// </summary>
        /// <param name="channel">The channel.</param>
        /// <param name="enabledCompressionOptions">The enabled compression options.</param>
        /// <param name="enabledEncryptionOptions">The enabled encryption options.</param>
        /// <param name="schemeOptions">The scheme options.</param>
        /// <param name="authenticateFunc">The authenticate function.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">
        /// </exception>
        /// <exception cref="System.ArgumentException">
        /// The transport doesn't support one or more of the specified compression options
        /// or
        /// The transport doesn't support one or more of the specified compression options
        /// or
        /// The authentication scheme options is mandatory
        /// </exception>
        public static async Task EstablishSessionAsync(
            this IServerChannel channel, 
            SessionCompression[] enabledCompressionOptions,
            SessionEncryption[] enabledEncryptionOptions,
            AuthenticationScheme[] schemeOptions,
            Func<Node, Authentication, AuthenticationResult> authenticateFunc,
            CancellationToken cancellationToken)
        {
            if (channel == null) throw new ArgumentNullException(nameof(channel));            
            if (enabledCompressionOptions == null || 
                enabledCompressionOptions.Length == 0 || 
                enabledCompressionOptions.Any(o => !channel.Transport.GetSupportedCompression().Contains(o)))
            {
                throw new ArgumentException("The transport doesn't support one or more of the specified compression options", nameof(enabledCompressionOptions));
            }

            if (enabledEncryptionOptions == null || 
                enabledEncryptionOptions.Length == 0 || 
                enabledEncryptionOptions.Any(o => !channel.Transport.GetSupportedEncryption().Contains(o)))
            {
                throw new ArgumentException("The transport doesn't support one or more of the specified compression options", nameof(enabledEncryptionOptions));
            }

            if (schemeOptions == null) throw new ArgumentNullException(nameof(schemeOptions));            
            if (schemeOptions.Length == 0) throw new ArgumentException("The authentication scheme options is mandatory", nameof(schemeOptions));
            if (authenticateFunc == null) throw new ArgumentNullException(nameof(authenticateFunc));

            // Awaits for the 'new' session envelope
            var receivedSession = await channel.ReceiveNewSessionAsync(cancellationToken).ConfigureAwait(false);
            if (receivedSession.State == SessionState.New)
            {
                // Check if there's any transport option to negotiate
                var compressionOptions = enabledCompressionOptions.Intersect(channel.Transport.GetSupportedCompression()).ToArray();
                var encryptionOptions = enabledEncryptionOptions.Intersect(channel.Transport.GetSupportedEncryption()).ToArray();
                
                if (compressionOptions.Length > 1 || encryptionOptions.Length > 1)
                {
                    // Negotiate the transport options
                    receivedSession = await channel.NegotiateSessionAsync(
                        compressionOptions,
                        encryptionOptions,
                        cancellationToken).ConfigureAwait(false);

                    // Validate the selected options
                    if (receivedSession.State == SessionState.Negotiating &&
                        receivedSession.Compression != null &&
                        compressionOptions.Contains(receivedSession.Compression.Value) &&
                        receivedSession.Encryption != null &&
                        encryptionOptions.Contains(receivedSession.Encryption.Value))
                    {
                        await channel.SendNegotiatingSessionAsync(
                            receivedSession.Compression.Value,
                            receivedSession.Encryption.Value, cancellationToken);

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
                    else
                    {
                        await channel.SendFailedSessionAsync(new Reason()
                        {
                            Code = ReasonCodes.SESSION_NEGOTIATION_INVALID_OPTIONS,
                            Description = "An invalid negotiation option was selected"
                        }, cancellationToken);
                    }
                }

                if (channel.State != SessionState.Failed)
                {
                    // Sends the authentication options and awaits for the authentication 
                    receivedSession = await channel.AuthenticateSessionAsync(schemeOptions, cancellationToken);
                    if (receivedSession.State == SessionState.Authenticating &&
                        receivedSession.Authentication != null &&
                        receivedSession.Scheme != null &&
                        schemeOptions.Contains(receivedSession.Scheme.Value))
                    {
                        while (channel.State == SessionState.Authenticating)
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            var authenticationResult = authenticateFunc(receivedSession.From, receivedSession.Authentication);
                            if (authenticationResult.Roundtrip != null)
                            {
                                receivedSession = await channel.AuthenticateSessionAsync(authenticationResult.Roundtrip, cancellationToken);
                            }
                            else if (authenticationResult.Node != null)
                            {
                                await channel.SendEstablishedSessionAsync(authenticationResult.Node, cancellationToken);
                            }
                            else
                            {
                                await channel.SendFailedSessionAsync(new Reason()
                                {
                                    Code = ReasonCodes.SESSION_AUTHENTICATION_FAILED,
                                    Description = "The session authentication failed"
                                }, cancellationToken);
                            }
                        }
                    }
                }
            }
        }
    }

    public sealed class AuthenticationResult
    {
        public AuthenticationResult(Authentication roundtrip, Node node)
        {
            Roundtrip = roundtrip;
            Node = node;
        }

        public Authentication Roundtrip { get; }

        public Node Node { get; }
    }
}
