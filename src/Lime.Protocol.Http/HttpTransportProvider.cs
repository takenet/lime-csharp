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
        #region Private Fields

        private readonly bool _useHttps;
        private readonly ConcurrentDictionary<string, ITransportSession> _transportDictionary;
        private readonly IEnvelopeStorage<Message> _messageStorage;
        private readonly IEnvelopeStorage<Notification> _notificationStorage;

        #endregion

        #region Constructor

        public HttpTransportProvider(bool useHttps, IEnvelopeStorage<Message> messageStorage, IEnvelopeStorage<Notification> notificationStorage)
        {
            _useHttps = useHttps;
            _messageStorage = messageStorage;
            _notificationStorage = notificationStorage;
            _transportDictionary = new ConcurrentDictionary<string, ITransportSession>();
        }

        #endregion

        #region IHttpTransportProvider Members

        public ITransportSession GetTransport(IPrincipal requestPrincipal, bool cacheInstance)
        {
            var identity = (HttpListenerBasicIdentity)requestPrincipal.Identity;
            var transportKey = GetTransportKey(identity);

            ITransportSession transport;

            if (cacheInstance)
            {
                transport = _transportDictionary.GetOrAdd(
                    transportKey,
                    k =>
                    {
                        var newTransport = CreateTransport(identity);
                        newTransport.Closing += (sender, e) =>
                        {
                            ITransportSession t;
                            _transportDictionary.TryRemove(k, out t);
                        };
                        return newTransport;
                    });
            }
            else if (!_transportDictionary.TryRemove(transportKey, out transport))
            {
                transport = CreateTransport(identity);
            }
            
            return transport;
        }

        public event EventHandler<TransportEventArgs> TransportCreated;

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
            TransportCreated.RaiseEvent(this, new TransportEventArgs(transport));
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
