using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using ReflectionMagic;

namespace Lime.Transport.SignalR
{
    internal static class HubContextExtensions
    {
        private static IHttpConnectionFeature GetHttpConnectionFeature<T>(IHubContext<T> hubContext, string connectionId) where T : Hub
        {
            var client = hubContext.Clients.Client(connectionId)?.AsDynamic();
            var connections = (System.Collections.Concurrent.ConcurrentDictionary<string, HubConnectionContext>)client?._lifetimeManager._connections._connections;
            if (connections is null)
                return null;

            if (connections.TryGetValue(connectionId, out var hubConnectionContext))
                return hubConnectionContext.Features.Get<IHttpConnectionFeature>();

            return null;
        }

        internal static EndPoint GetRemoteEndpoint<T>(this IHubContext<T> hubContext, string connectionId) where T : Hub
        {
            var httpConnectionFeature = GetHttpConnectionFeature(hubContext, connectionId);
            var remoteEndpoint = (EndPoint)httpConnectionFeature?.AsDynamic().RemoteEndPoint;

            return remoteEndpoint;
        }

        internal static EndPoint GetLocalEndpoint<T>(this IHubContext<T> hubContext, string connectionId) where T : Hub
        {
            var httpConnectionFeature = GetHttpConnectionFeature(hubContext, connectionId);
            var localEndpoint = (EndPoint)httpConnectionFeature?.AsDynamic().LocalEndPoint;

            return localEndpoint;
        }
    }
}
