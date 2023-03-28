using System.Diagnostics;

namespace Lime.Transport.WebSocket
{
    /// <summary>
    /// Provides the ActivitySource for the Lime.Protocol WebSocket transport
    /// </summary>
    public static class LimeWebSocketActivitySource
    {
        /// <summary>
        /// The name of the ActivitySource for the Lime.Protocol WebSocket transport
        /// </summary>
        public const string LimeWebSocket = "Lime.Transport.WebSocket";

        /// <summary>
        /// Gets the ActivitySource for the Lime.Protocol to be used when creating activities.
        /// </summary>
        public static readonly ActivitySource Instance = new ActivitySource(LimeWebSocket);
    }
}