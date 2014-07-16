using Lime.Protocol;
using Lime.Protocol.Network;
using Lime.Protocol.Serialization;
using Lime.Protocol.Server;
using Lime.Protocol.Tcp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol.Security;
using System.Security.Cryptography;
using System.Security.AccessControl;
using System.Security.Principal;
using Notification = Lime.Protocol.Notification;

namespace Lime.Client.Console
{
    public class ConsoleServer
    {
        private static Uri _listenerUri;
        private ITransportListener _listener;
        private IDictionary<Guid, IServerChannel> _serverConnectedNodesDictionary;

        private IDictionary<Identity, IDictionary<string, Guid>> _identityInstanceSessionIdDictionary;

        private IDictionary<Identity, string> _identityPasswordDictionary;
        private Node _serverNode;

        #region Constructor
        
        public ConsoleServer(Uri listenerUri)
        {
            _serverNode = new Node() 
            { 
                Name = "server", 
                Domain = "limeprotocol.org", 
                Instance = Environment.MachineName 
            };

            _listenerUri = listenerUri;
            _serverConnectedNodesDictionary = new Dictionary<Guid, IServerChannel>();
            _identityInstanceSessionIdDictionary = new Dictionary<Identity, IDictionary<string, Guid>>();

            _identityPasswordDictionary = new Dictionary<Identity, string>
            {
                { Identity.Parse("ww@limeprotocol.org") , "123456" },
                { Identity.Parse("skylar@limeprotocol.org") , "abcdef" },
                { Identity.Parse("wjr@limeprotocol.org") , "654321" },
                { Identity.Parse("hank@limeprotocol.org") , "minerals" },
            };
        }

        #endregion

        public async Task StartServerAsync()
        {
            var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadOnly);

            //var certificates = store.Certificates.Find(X509FindType.FindByThumbprint, "f864d23e92894c56df566b7ab7a9c6411d50d14d", false);
            var certificates = store.Certificates.Find(X509FindType.FindByThumbprint, "3fd0b5f28dc32f5f0bb9f44cf1f6816846e44cbe", false);
            
            if (certificates.Count == 0)
            {
                throw new InvalidOperationException("Server certificate not found");
            }

            store.Close();
#if DEBUG
            ITraceWriter traceWriter = new DebugTraceWriter("Server"); 
#else
            ITraceWriter traceWriter = new FileTraceWriter("server.log"); 
#endif

            _listener = new TcpTransportListener(
                _listenerUri,
                certificates[0],
                new EnvelopeSerializer(),
                traceWriter
                );


            await _listener.StartAsync();
        }

        public async Task StopServerAsync()
        {
            await _listener.StopAsync();
        }

        public async Task ListenAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var transport = await _listener.AcceptTransportAsync(cancellationToken);

                var serverChannel = new ServerChannel(
                    Guid.NewGuid(),
                    _serverNode,
                    transport,
                    TimeSpan.FromSeconds(60));

                await serverChannel.Transport.OpenAsync(_listenerUri, cancellationToken);                    

                this.EstablishSessionAsync(serverChannel, cancellationToken);                
            }
        }

        private async Task EstablishSessionAsync(IServerChannel channel, CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                _serverConnectedNodesDictionary.Add(channel.SessionId, channel);

                var timeoutToken = new CancellationTokenSource(TimeSpan.FromSeconds(60));

                var newSession = await channel.ReceiveNewSessionAsync(
                    CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutToken.Token).Token);

                timeoutToken = new CancellationTokenSource(TimeSpan.FromSeconds(60));

                var negotiatedSession = await channel.NegotiateSessionAsync(
                    channel.Transport.GetSupportedCompression(),
                    channel.Transport.GetSupportedEncryption(),
                    CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutToken.Token).Token);

                if (negotiatedSession.State == SessionState.Negotiating &&
                    negotiatedSession.Compression != null &&
                    negotiatedSession.Encryption != null)
                {                    
                    await channel.SendNegotiatingSessionAsync(
                        negotiatedSession.Compression.Value,
                        negotiatedSession.Encryption.Value
                        );

                    timeoutToken = new CancellationTokenSource(TimeSpan.FromSeconds(60));

                    if (channel.Transport.Compression != negotiatedSession.Compression.Value)
                    {
                        await channel.Transport.SetCompressionAsync(
                            negotiatedSession.Compression.Value,
                            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutToken.Token).Token);
                    }

                    if (channel.Transport.Encryption != negotiatedSession.Encryption.Value)
                    {
                        await channel.Transport.SetEncryptionAsync(
                            negotiatedSession.Encryption.Value,
                            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutToken.Token).Token);
                    }

                    timeoutToken = new CancellationTokenSource(TimeSpan.FromSeconds(60));

                    var authenticatedSession = await channel.AuthenticateSessionAsync(
                        new AuthenticationScheme[] { AuthenticationScheme.Plain, AuthenticationScheme.Transport },
                        CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutToken.Token).Token);

                    if (authenticatedSession.Authentication != null &&
                        authenticatedSession.From != null &&
                        authenticatedSession.From.Domain.Equals(_serverNode.Domain, StringComparison.OrdinalIgnoreCase))
                    {
                        if (authenticatedSession.Authentication is PlainAuthentication)
                        {
                            var plainAuthentication = authenticatedSession.Authentication as PlainAuthentication;

                            string password;

                            if (_identityPasswordDictionary.TryGetValue(authenticatedSession.From.ToIdentity(), out password) &&
                                password.Equals(plainAuthentication.GetFromBase64Password()))
                            {
                                await RegisterChannel(channel, cancellationToken, authenticatedSession.From);
                            }
                            else
                            {
                                await channel.SendFailedSessionAsync(
                                    new Reason()
                                    {
                                        Code = ReasonCodes.SESSION_AUTHENTICATION_FAILED,
                                        Description = "Invalid username or password"
                                    });
                            }

                        }
                        else if (authenticatedSession.Authentication is TransportAuthentication)
                        {
                            var transportAuthentication = authenticatedSession.Authentication as PlainAuthentication;

                            if (channel.Transport is IAuthenticatableTransport)
                            {
                                var authenticableTransport = channel.Transport as IAuthenticatableTransport;

                                if (await authenticableTransport.AuthenticateAsync(authenticatedSession.From.ToIdentity()) != DomainRole.Unknown)
                                {
                                    await RegisterChannel(channel, cancellationToken, authenticatedSession.From);
                                }
                                else
                                {
                                    await channel.SendFailedSessionAsync(
                                        new Reason()
                                        {
                                            Code = ReasonCodes.SESSION_AUTHENTICATION_FAILED,
                                            Description = "The authentication failed"
                                        });
                                }

                            }
                            else
                            {
                                await channel.SendFailedSessionAsync(
                                    new Reason()
                                    {
                                        Code = ReasonCodes.SESSION_AUTHENTICATION_FAILED,
                                        Description = "The current transport doesn't support authentication"
                                    });
                            }

                        }
                        else
                        {
                            await channel.SendFailedSessionAsync(
                                new Reason()
                                {
                                    Code = ReasonCodes.SESSION_AUTHENTICATION_FAILED,
                                    Description = "Unsupported authenticaiton scheme"
                                });
                        }
                    }
                    else
                    {
                        await channel.SendFailedSessionAsync(
                            new Reason()
                            {
                                Code = ReasonCodes.SESSION_AUTHENTICATION_FAILED,
                                Description = "Invalid user"
                            });
                    }
                }
                else
                {
                    await channel.SendFailedSessionAsync(
                        new Reason()
                        {
                            Code = 1,
                            Description = "Invalid selected negotiation options"
                        });
                }
            }
            finally
            {
                channel.DisposeIfDisposable();
            }            
        }

        private async Task RegisterChannel(IServerChannel channel, CancellationToken cancellationToken, Node node)
        {
            IDictionary<string, Guid> instanceSessionDictionary;

            if (!_identityInstanceSessionIdDictionary.TryGetValue(node.ToIdentity(), out instanceSessionDictionary))
            {
                instanceSessionDictionary = new Dictionary<string, Guid>();
                _identityInstanceSessionIdDictionary.Add(node.ToIdentity(), instanceSessionDictionary);
            }

            instanceSessionDictionary.Add(node.Instance, channel.SessionId);

            await channel.SendEstablishedSessionAsync(node);

            var receiveMessageTask = this.ReceiveMessagesAsync(channel, cancellationToken);

            await channel.ReceiveFinishingSessionAsync(cancellationToken);

            await channel.SendFinishedSessionAsync();

            instanceSessionDictionary.Remove(node.Instance);

            if (instanceSessionDictionary.Count == 0)
            {
                _identityInstanceSessionIdDictionary.Remove(node.ToIdentity());
            }
        }

        private async Task ReceiveMessagesAsync(IChannel channel, CancellationToken cancellationToken)
        {
            while (channel.State == SessionState.Established)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var message = await channel.ReceiveMessageAsync(cancellationToken);

                IDictionary<string, Guid> instanceSessionDictionary;

                if (message.To == null)
                {
                    var notification = new Notification()
                    {
                        Id = message.Id,
                        Event = Event.Failed,
                        Reason = new Reason()
                        {
                            Code = ReasonCodes.VALIDATION_INVALID_RECIPIENTS,
                            Description = "Invalid destination"
                        }
                    };

                    await channel.SendNotificationAsync(notification);
                }
                else if (!_identityInstanceSessionIdDictionary.TryGetValue(message.To.ToIdentity(), out instanceSessionDictionary) ||
                         !instanceSessionDictionary.Any())
                {
                    var notification = new Notification()
                    {
                        Id = message.Id,
                        Event = Event.Failed,
                        Reason = new Reason()
                        {
                            Code = ReasonCodes.ROUTING_DESTINATION_NOT_FOUND,
                            Description = "Destination not found"
                        }
                    };

                    await channel.SendNotificationAsync(notification);
                }
                else 
                {
                    Guid destinationSessionId;

                    if (!instanceSessionDictionary.TryGetValue(message.To.Instance, out destinationSessionId))
                    {
                        destinationSessionId = instanceSessionDictionary.First().Value;
                    }

                    IServerChannel destinationChannel;

                    if (_serverConnectedNodesDictionary.TryGetValue(destinationSessionId, out destinationChannel))
                    {
                        await destinationChannel.SendMessageAsync(message);
                    }
                    else
                    {
                        var notification = new Notification()
                        {
                            Id = message.Id,
                            Event = Event.Failed,
                            Reason = new Reason()
                            {
                                Code = ReasonCodes.DISPATCH_ERROR,
                                Description = "Destination session is unavailable"
                            }
                        };

                        await channel.SendNotificationAsync(notification);
                    }
                }

            }
        }



        
    }
}
