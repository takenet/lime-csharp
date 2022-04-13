using System;
using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.SignalR.Client;
using ReflectionMagic;

namespace Lime.Transport.SignalR
{
    internal static class HubConnectionExtensions
    {
        private static Socket GetWebSocket(HubConnection hubConnection)
        {
            dynamic transport = hubConnection.AsDynamic()._state?.CurrentConnectionStateUnsynchronized?.Connection?._transport;
            if (transport is null)
                return null;

            var transportName = ((object)transport).GetType().Name;
            if (transportName.Contains("WebSocket", StringComparison.InvariantCulture))
                return null;
#if NET6_0
            // For .net 6
            return (Socket)transport?._webSocket?._innerWebSocket?.WebSocket?._stream?._connection?._socket;
#else
            // For .net 3.1
            return (Socket)transport?._webSocket?._innerWebSocket?._webSocket?._stream?._connection?._socket;
#endif
        }

        public static EndPoint GetRemoteEndpoint(this HubConnection hubConnection)
        {
            return GetWebSocket(hubConnection)?.RemoteEndPoint;
        }

        public static EndPoint GetLocalEndpoint(this HubConnection hubConnection)
        {
            return GetWebSocket(hubConnection)?.LocalEndPoint;
        }
    }
}
