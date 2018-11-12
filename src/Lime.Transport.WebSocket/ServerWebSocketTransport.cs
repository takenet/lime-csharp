using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol.Network;
using Lime.Protocol.Serialization;

namespace Lime.Transport.WebSocket
{
    public class ServerWebSocketTransport : WebSocketTransport
    {
        private readonly HttpListenerWebSocketContext _context;

        internal ServerWebSocketTransport(
            HttpListenerWebSocketContext context,
            IEnvelopeSerializer envelopeSerializer,
            ITraceWriter traceWriter = null,
            int bufferSize = 8192,
            WebSocketMessageType webSocketMessageType = WebSocketMessageType.Text)
            : base(context.WebSocket, envelopeSerializer, traceWriter, bufferSize, webSocketMessageType)
        {
            _context = context;
        }

        public override IReadOnlyDictionary<string, object> Options
        {
            get
            {
                var options = (Dictionary<string, object>)base.Options;
                options.Add(nameof(HttpListenerWebSocketContext.IsAuthenticated), _context.IsAuthenticated);
                options.Add(nameof(HttpListenerWebSocketContext.IsLocal), _context.IsLocal);
                options.Add(nameof(HttpListenerWebSocketContext.IsSecureConnection), _context.IsSecureConnection);
                options.Add(nameof(HttpListenerWebSocketContext.Origin), _context.Origin);
                options.Add(nameof(HttpListenerWebSocketContext.SecWebSocketKey), _context.SecWebSocketKey);
                options.Add(nameof(HttpListenerWebSocketContext.SecWebSocketVersion), _context.SecWebSocketVersion);

                if (_context.Headers != null &&
                    _context.Headers.Count > 0)
                {
                    var headersBuilder = new StringBuilder();
                    foreach (var key in _context.Headers.AllKeys.Where(o => !options.ContainsKey(o)))
                    {
                        headersBuilder.AppendFormat("{0}={1};", key, _context.Headers[key]);
                    }
                    options.Add(nameof(HttpListenerWebSocketContext.Headers), headersBuilder.ToString().TrimEnd(';'));
                }

                if (_context.CookieCollection != null &&
                    _context.CookieCollection.Count > 0)
                {
                    var cookiesBuilder = new StringBuilder();

                    foreach (Cookie cookie in _context.CookieCollection)
                    {
                        cookiesBuilder.AppendFormat("{0}={1};", cookie.Name, cookie.Value);
                    }

                    options.Add(nameof(HttpListenerWebSocketContext.CookieCollection), cookiesBuilder.ToString().TrimEnd(';'));
                }

                return options;
            }
        }

        protected override Task PerformOpenAsync(Uri uri, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    internal static class ReflectionExtensions
    {
        public static object GetFieldValue(this object obj, string name)
        {
            // Set the flags so that private and public fields from instances will be found
            var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy;
            var field = obj.GetType().GetField(name, bindingFlags);
            return field?.GetValue(obj);
        }
    }
}