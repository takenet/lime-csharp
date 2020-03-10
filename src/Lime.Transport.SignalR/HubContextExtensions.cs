using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using ReflectionMagic;

namespace Lime.Transport.SignalR
{
    internal static class HubContextExtensions
    {
        private static IHttpConnectionFeature GetHttpConnectionFeature<T>(IHubContext<T> hubContext, string userId) where T : Hub
        {
            var client = hubContext.Clients.Client(userId)?.AsDynamic();
            var connections = (System.Collections.Concurrent.ConcurrentDictionary<string, HubConnectionContext>)client?._lifetimeManager._connections._connections;
            return connections.FirstOrDefault(kv => kv.Value.UserIdentifier == userId).Value?.Features.Get<IHttpConnectionFeature>();
        }

        internal static EndPoint GetRemoteEndpoint<T>(this IHubContext<T> hubContext, string userId) where T : Hub
        {
            var httpConnectionFeature = GetHttpConnectionFeature(hubContext, userId);
            var remoteEndpoint = (EndPoint)httpConnectionFeature?.AsDynamic().RemoteEndPoint;

            return remoteEndpoint;
        }

        internal static EndPoint GetLocalEndpoint<T>(this IHubContext<T> hubContext, string userId) where T : Hub
        {
            var httpConnectionFeature = GetHttpConnectionFeature(hubContext, userId);
            var localEndpoint = (EndPoint)httpConnectionFeature?.AsDynamic().LocalEndPoint;

            return localEndpoint;
        }
    }
}
