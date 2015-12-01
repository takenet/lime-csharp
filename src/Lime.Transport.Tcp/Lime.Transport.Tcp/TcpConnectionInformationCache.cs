using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;

namespace Lime.Transport.Tcp
{
    internal static class TcpConnectionInformationCache
    {
        private static Dictionary<Tuple<IPEndPoint, IPEndPoint>, TcpConnectionInformation> _endpointConnectionDictionary;
        private static object _syncRoot = new object();
        private static DateTimeOffset _expiration;

        private static readonly TimeSpan CacheExpirationInterval = TimeSpan.FromSeconds(30);

        static TcpConnectionInformationCache()
        {
            _endpointConnectionDictionary = new Dictionary<Tuple<IPEndPoint, IPEndPoint>, TcpConnectionInformation>();
            _expiration = DateTimeOffset.MinValue;
        }

        public static TcpConnectionInformation GetTcpConnectionInformation(IPEndPoint localEndPoint, IPEndPoint remoteEndPoint)
        {
            var key = new Tuple<IPEndPoint, IPEndPoint>(localEndPoint, remoteEndPoint);

            TcpConnectionInformation connectionInformation;

            // Check if the cache is valid
            if (_expiration >= DateTimeOffset.UtcNow &&
                _endpointConnectionDictionary.TryGetValue(key, out connectionInformation))
            {
                return connectionInformation;
            }

            // Update the cache and check for the value
            lock (_syncRoot)
            {
                // Try get the value again in the critical region
                if (_expiration >= DateTimeOffset.UtcNow &&
                    _endpointConnectionDictionary.TryGetValue(key, out connectionInformation))
                {
                    return connectionInformation;
                }
                var activeTcpConnections = IPGlobalProperties
                    .GetIPGlobalProperties()
                    .GetActiveTcpConnections();

                _endpointConnectionDictionary = activeTcpConnections
                    .ToDictionary(e => new Tuple<IPEndPoint, IPEndPoint>(e.LocalEndPoint, e.RemoteEndPoint), e => e);

                _expiration = DateTimeOffset.UtcNow.Add(CacheExpirationInterval);
            }

            // Finally, try to find the updated value
            if (_endpointConnectionDictionary.TryGetValue(key, out connectionInformation))
            {
                return connectionInformation;
            }

            return null;
        }
    }
}
