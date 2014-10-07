using Lime.Protocol.Http.Storage;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Security.Principal;
using System.Text;

namespace Lime.Protocol.Http
{
    public sealed class HttpTransportProvider : IHttpTransportProvider
    {
        private readonly bool _useHttps;
        private readonly ConcurrentDictionary<string, ServerHttpTransport> _transportDictionary;

        private readonly IEnvelopeStorage<Message> _messageStorage;
        private readonly IEnvelopeStorage<Notification> _notificationStorage;

        public HttpTransportProvider(bool useHttps, IEnvelopeStorage<Message> messageStorage, IEnvelopeStorage<Notification> notificationStorage)
        {
            _useHttps = useHttps;
            _messageStorage = messageStorage;
            _notificationStorage = notificationStorage;
            _transportDictionary = new ConcurrentDictionary<string, ServerHttpTransport>();
        }

        #region IHttpTransportProvider Members

        public ServerHttpTransport GetTransport(IPrincipal requestPrincipal)
        {
            var identity = (HttpListenerBasicIdentity)requestPrincipal.Identity;
            var transportKey = GetTransportKey(identity);

            var transport = _transportDictionary.GetOrAdd(
                transportKey,
                k =>
                {
                    var newTransport = CreateTransport(identity);
                    newTransport.Closing += (sender, e) =>
                    {
                        _transportDictionary.TryRemove(k, out newTransport);
                    };
                    return newTransport;
                });
            return transport;
        }

        public event EventHandler<ServerHttpTransportEventArgs> TransportCreated;

        #endregion

        /// <summary>
        /// Creates a new instance
        /// of tranport for the
        /// specified identity
        /// </summary>
        /// <param name="identity"></param>
        /// <returns></returns>
        private ServerHttpTransport CreateTransport(HttpListenerBasicIdentity identity)
        {
            var transport = new ServerHttpTransport(identity, _useHttps, _messageStorage, _notificationStorage);
            TransportCreated.RaiseEvent(this, new ServerHttpTransportEventArgs(transport));
            return transport;
        }

        /// <summary>
        /// Gets a hashed key based on
        /// the identity and password.
        /// </summary>
        /// <param name="identity"></param>
        /// <returns></returns>
        private static string GetTransportKey(HttpListenerBasicIdentity identity)
        {
            return string.Format("{0}:{1}", identity.Name, identity.Password).ToSHA1HashString();
        }
    }
}
