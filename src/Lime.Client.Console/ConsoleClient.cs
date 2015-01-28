using Lime.Protocol;
using Lime.Protocol.Client;
using Lime.Messaging.Contents;
using Lime.Protocol.Network;
using Lime.Messaging.Resources;
using Lime.Protocol.Security;
using Lime.Protocol.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lime.Transport.Tcp.Protocol;

namespace Lime.Client.Console
{
    public sealed class ConsoleClient : IDisposable
    {
        private static Uri _clientUri;
        public IClientChannel Channel { get; private set; }

        public ConsoleClient(Uri clientUri)
        {
            _clientUri = clientUri;
        }

        public async Task<bool> ConnectAsGuestAsync(string domain, CancellationToken cancellationToken)
        {
            var tcpClient = new TcpClient();

#if DEBUG
            ITraceWriter traceWriter = new DebugTraceWriter("Client");
#else
            ITraceWriter traceWriter = new FileTraceWriter("client.log");
#endif

            var transport = new TcpTransport(
                new TcpClientAdapter(tcpClient),
                new EnvelopeSerializer(),
                hostName: _clientUri.Host,
                traceWriter: traceWriter
                );

            Channel = new ClientChannel(transport, TimeSpan.FromSeconds(60), autoReplyPings: true, autoNotifyReceipt: true);

            await Channel.Transport.OpenAsync(_clientUri, cancellationToken);

            var identity = new Identity()
            {
                Name = Guid.NewGuid().ToString(),
                Domain = domain
            };

            var resultSession = await Channel.EstablishSessionAsync(
                c => c.First(),
                e => SessionEncryption.TLS,
                identity,
                (s, r) => new GuestAuthentication(),
                Environment.MachineName,
                cancellationToken);

            if (resultSession.State != SessionState.Established)
            {
                return false;
            }

            return true;
        }

        public async Task<bool> ConnectWithPasswordAsync(Identity identity, string password, CancellationToken cancellationToken)
        {
            var tcpClient = new TcpClient();

#if DEBUG
            ITraceWriter traceWriter = new DebugTraceWriter("Client"); 
#else
            ITraceWriter traceWriter = new FileTraceWriter("client.log");
#endif

            var transport = new TcpTransport(
                new TcpClientAdapter(tcpClient),
                new EnvelopeSerializer(),                
                hostName: _clientUri.Host,
                traceWriter: traceWriter
                );

            Channel = new ClientChannel(transport, TimeSpan.FromSeconds(60), autoReplyPings: true, autoNotifyReceipt: true);
            
            await Channel.Transport.OpenAsync(_clientUri, cancellationToken);

            var resultSession = await Channel.EstablishSessionAsync(
                c => c.First(),
                e => SessionEncryption.TLS,
                identity,
                (s, r) => { var auth = new PlainAuthentication(); auth.SetToBase64Password(password); return auth; },
                Environment.MachineName,
                cancellationToken);

            if (resultSession.State != SessionState.Established)            
            {
                return false;
            }

            return true;
        }

        public async Task<bool> ConnectWithCertificateAsync(Identity identity, X509Certificate2 clientCertificate, CancellationToken cancellationToken)
        {
            var tcpClient = new TcpClient();

#if DEBUG
            ITraceWriter traceWriter = new DebugTraceWriter("Client");
#else
            ITraceWriter traceWriter = new FileTraceWriter("client.log");
#endif

            var transport = new TcpTransport(
                new TcpClientAdapter(tcpClient),
                new EnvelopeSerializer(),
                hostName: _clientUri.Host,
                clientCertificate: clientCertificate,
                traceWriter: traceWriter
                );

            Channel = new ClientChannel(transport, TimeSpan.FromSeconds(60), autoReplyPings: true, autoNotifyReceipt: true);

            await Channel.Transport.OpenAsync(_clientUri, cancellationToken);

            var resultSession = await Channel.EstablishSessionAsync(
                c => c.First(),
                e => SessionEncryption.TLS,
                identity,
                (s, r) => new TransportAuthentication(),
                Environment.MachineName,
                cancellationToken);

            if (resultSession.State != SessionState.Established)
            {
                return false;
            }

            return true;
        }

        public async Task ReceiveMessagesAsync(CancellationToken cancellationToken)
        {
            while (Channel.State == SessionState.Established)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var message = await Channel.ReceiveMessageAsync(cancellationToken);
                                
                System.Console.WriteLine("Message received from '{0}': {1}", message.From, message.Content);                
            }
        }

        public async Task ReceiveNotificationsAsync(CancellationToken cancellationToken)
        {
            while (Channel.State == SessionState.Established)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var notification = await Channel.ReceiveNotificationAsync(cancellationToken);

                if (notification.Reason != null)
                {
                    System.Console.WriteLine("Notification received from '{0}': {1} - Reason: {2}", notification.From, notification.Event, notification.Reason.Description);
                }
                else
                {
                    System.Console.WriteLine("Notification received from '{0}': {1}", notification.From, notification.Event);
                }                
            }
        }

        public async Task DisconnectAsync(CancellationToken cancellationToken)
        {            
            await Channel.SendFinishingSessionAsync();
            await Channel.ReceiveFinishedSessionAsync(cancellationToken);
        }

        public Task<Command> SetReceiptsAsync(CancellationToken cancellationToken)
        {
            var receiptCommand = new Command()
            {
                Method = CommandMethod.Set,
                Resource = new Receipt()
                {
                    Events = new[] { Event.Dispatched, Event.Received }
                }
            };
            return Channel.ProcessCommandAsync(receiptCommand, cancellationToken);            
        }

        public Task<Command> GetAccountAsync(CancellationToken cancellationToken)
        {
            var accountCommand = new Command
            {
                Method = CommandMethod.Get,
                Resource = new Account()
            };
            return Channel.ProcessCommandAsync(accountCommand, cancellationToken);
        }

        public Task<Command> SetAccountAsync(Account account, CancellationToken cancellationToken)
        {
            var accountCommand = new Command
            {
                Method = CommandMethod.Set,
                Resource = account
            };

            return Channel.ProcessCommandAsync(accountCommand, cancellationToken);
        }

        public Task<Command> GetContactsAsync(CancellationToken cancellationToken)
        {
            var rosterCommand = new Command
            {
                Method = CommandMethod.Get,
                Uri = LimeUri.Parse(UriTemplates.CONTACTS)
            };
            return Channel.ProcessCommandAsync(rosterCommand, cancellationToken);
        }

        public Task<Command> SetContactsAsync(Contact contact, CancellationToken cancellationToken)
        {
            var contactCommand = new Command
            {
                Method = CommandMethod.Set,
                Uri = LimeUri.Parse(UriTemplates.CONTACTS),
                Resource = contact
            };

            return Channel.ProcessCommandAsync(contactCommand, cancellationToken);
        }


        public Task<Command> SetPresenceAsync(Presence presence, CancellationToken cancellationToken)
        {
            var presenceCommand = new Command()
            {
                Method = CommandMethod.Set,
                Resource = presence
            };
            return Channel.ProcessCommandAsync(presenceCommand, cancellationToken);
        }

        public Task SendTextMessageAsync(Node to, string text)
        {
            var message = new Message()
            {
                To = to,
                Content = new PlainText()
                {
                    Text = text
                }
            };

            return Channel.SendMessageAsync(message);
        }


        #region IDisposable Members

        public void Dispose()
        {
            if (this.Channel != null)
            {
                this.Channel.DisposeIfDisposable();
            }
        }

        #endregion
    }
}
