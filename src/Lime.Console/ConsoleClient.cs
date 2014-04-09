using Lime.Protocol;
using Lime.Protocol.Client;
using Lime.Protocol.Security;
using Lime.Protocol.Serialization;
using Lime.Protocol.Tcp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lime.Console
{
    public class ConsoleClient
    {
        private static Uri _clientUri;
        private IClientChannel _channel;

        public ConsoleClient(Uri clientUri)
        {
            _clientUri = clientUri;
        }


        public async Task<bool> ConnectAsync(Identity identity, string password, CancellationToken cancellationToken)
        {
            var tcpClient = new TcpClient();

            var transport = new TcpTransport(
                new TcpClientAdapter(tcpClient),
                new EnvelopeSerializer(),
                traceWriter: new DebugTraceWriter("Client"),
                hostName: _clientUri.Host
                );

            _channel = new ClientChannel(transport, TimeSpan.FromSeconds(60));
            
            await _channel.Transport.OpenAsync(_clientUri, cancellationToken);

            var negotiateOrAuthenticateSession = await _channel.StartNewSessionAsync(cancellationToken);

            Session authenticatingSession;

            if (negotiateOrAuthenticateSession.State == SessionState.Negotiating)
            {
                var confirmedNegotiateSession = await _channel.NegotiateSessionAsync(negotiateOrAuthenticateSession.CompressionOptions.First(), negotiateOrAuthenticateSession.EncryptionOptions.Last(), cancellationToken);

                if (_channel.Transport.Compression != confirmedNegotiateSession.Compression.Value)
                {
                    await _channel.Transport.SetCompressionAsync(
                        confirmedNegotiateSession.Compression.Value,
                        cancellationToken);
                }

                if (_channel.Transport.Encryption != confirmedNegotiateSession.Encryption.Value)
                {
                    await _channel.Transport.SetEncryptionAsync(
                        confirmedNegotiateSession.Encryption.Value, 
                        cancellationToken);
                }

                authenticatingSession = await _channel.ReceiveAuthenticatingSessionAsync(cancellationToken);
            }
            else
            {
                authenticatingSession = negotiateOrAuthenticateSession;
            }
            
            if (authenticatingSession.State != SessionState.Authenticating)
            {
                return false;
            }

            var authentication = new PlainAuthentication();
            authentication.SetToBase64Password(password);

            var authenticationResultSession = await _channel.AuthenticateSessionAsync(
                new Identity() { Name = identity.Name, Domain = identity.Domain },
                authentication,
                Environment.MachineName,
                SessionMode.Node,
                cancellationToken);

            if (authenticationResultSession.State != SessionState.Established)            
            {
                return false;
            }

            return true;
        }

        public async Task ReceiveMessagesAsync(CancellationToken cancellationToken)
        {
            while (_channel.State == SessionState.Established)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var message = await _channel.ReceiveMessageAsync(cancellationToken);
                                
                System.Console.WriteLine("Message received from '{0}': {1}", message.From, message.Content);                
            }
        }
        
        public async Task Disconnect(CancellationToken cancellationToken)
        {
            await _channel.SendFinishingSessionAsync();
            await _channel.ReceiveFinishedSessionAsync(cancellationToken);
        }

    }
}
