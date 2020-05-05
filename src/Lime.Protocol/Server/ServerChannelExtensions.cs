using Lime.Protocol.Security;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Lime.Protocol.Server
{
    public static class ServerChannelExtensions
    {
        /// <summary>
        /// Establishes a server channel with transport options negotiation and authentication.
        /// </summary>
        public static async Task EstablishSessionAsync(
            this IServerChannel channel, 
            SessionCompression[] enabledCompressionOptions,
            SessionEncryption[] enabledEncryptionOptions,
            AuthenticationScheme[] schemeOptions,
            Func<Identity, Authentication, CancellationToken, Task<AuthenticationResult>> authenticationFunc,
            Func<Node, IServerChannel, CancellationToken, Task<Node>> registrationFunc,
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
            if (authenticationFunc == null) throw new ArgumentNullException(nameof(authenticationFunc));
            if (registrationFunc == null) throw new ArgumentNullException(nameof(registrationFunc));

            // Awaits for the 'new' session envelope
            var receivedSession = await channel.ReceiveNewSessionAsync(cancellationToken).ConfigureAwait(false);
            if (receivedSession.State == SessionState.New)
            {
                // Check if there's any transport option to negotiate
                var compressionOptions = enabledCompressionOptions.Intersect(channel.Transport.GetSupportedCompression()).ToArray();
                var encryptionOptions = enabledEncryptionOptions.Intersect(channel.Transport.GetSupportedEncryption()).ToArray();
                
                if (compressionOptions.Length > 1 || 
                    encryptionOptions.Length > 1)
                {
                    await NegotiateSessionAsync(channel, compressionOptions, encryptionOptions, cancellationToken).ConfigureAwait(false);
                }

                if (channel.State != SessionState.Failed)
                {
                    await AuthenticateSessionAsync(channel, schemeOptions, authenticationFunc, registrationFunc, cancellationToken).ConfigureAwait(false);
                }
            }

            if (channel.State < SessionState.Established)
            {
                await channel.SendFailedSessionAsync(
                    new Reason()
                    {
                        Code = ReasonCodes.SESSION_ERROR,
                        Description = "The session establishment failed"
                    }, 
                    cancellationToken);
            }
        }

        private static async Task NegotiateSessionAsync(
            IServerChannel channel,
            SessionCompression[] compressionOptions,
            SessionEncryption[] encryptionOptions,
            CancellationToken cancellationToken)
        {
            // Negotiate the transport options
            var receivedSession = await channel.NegotiateSessionAsync(
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
                await channel.SendFailedSessionAsync(
                    new Reason()
                    {
                        Code = ReasonCodes.SESSION_NEGOTIATION_INVALID_OPTIONS,
                        Description = "An invalid negotiation option was selected"
                    }, 
                    cancellationToken);
            }
        }
        
        private static async Task AuthenticateSessionAsync(
            IServerChannel channel,
            AuthenticationScheme[] schemeOptions,
            Func<Identity, Authentication, CancellationToken, Task<AuthenticationResult>> authenticationFunc,
            Func<Node, IServerChannel, CancellationToken, Task<Node>> registrationFunc,
            CancellationToken cancellationToken)
        {
            // Sends the authentication options and awaits for the authentication 
            var receivedSession = await channel.AuthenticateSessionAsync(schemeOptions, cancellationToken);
            
            while (receivedSession.State == SessionState.Authenticating &&
                   receivedSession.From != null &&
                   receivedSession.Authentication != null &&
                   receivedSession.Scheme != null &&
                   schemeOptions.Contains(receivedSession.Scheme.Value))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (receivedSession.Authentication is TransportAuthentication transportAuthentication)
                {
                    await AuthenticateAsTransportAsync(channel, transportAuthentication, receivedSession.From.ToIdentity());
                }

                var authenticationResult = await authenticationFunc(
                    receivedSession.From.ToIdentity(),
                    receivedSession.Authentication,
                    cancellationToken);

                if (authenticationResult.DomainRole != DomainRole.Unknown)
                {
                    var registeredNode = await registrationFunc(receivedSession.From, channel, cancellationToken);
                    if (registeredNode != null)
                    {
                        await channel.SendEstablishedSessionAsync(registeredNode, cancellationToken);
                    }
                    else
                    {
                        await channel.SendFailedSessionAsync(
                            new Reason()
                            {
                                Code = ReasonCodes.SESSION_REGISTRATION_ERROR,
                                Description = "The session instance registration failed"
                            }, 
                            cancellationToken);
                    }

                    break;
                }
                
                if (authenticationResult.Roundtrip != null)
                {
                    receivedSession =
                        await channel.AuthenticateSessionAsync(authenticationResult.Roundtrip, cancellationToken);
                }
                else
                {
                    await channel.SendFailedSessionAsync(
                        new Reason()
                        {
                            Code = ReasonCodes.SESSION_AUTHENTICATION_FAILED,
                            Description = "The session authentication failed"
                        }, 
                        cancellationToken);

                    break;
                }
            }
        }

        private static async Task AuthenticateAsTransportAsync(
            IServerChannel channel,
            TransportAuthentication transportAuthentication,
            Identity identity)
        {
            // Ensure that the domain role value is null.
            transportAuthentication.DomainRole = null;

            if (channel.Transport is IAuthenticatableTransport authenticatableTransport)
            {
                transportAuthentication.DomainRole = await authenticatableTransport.AuthenticateAsync(identity);
            }
        }
    }
}
