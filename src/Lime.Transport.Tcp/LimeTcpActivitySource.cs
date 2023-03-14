using System.Diagnostics;

namespace Lime.Transport.Tcp
{
    /// <summary>
    /// Provides the ActivitySource for the Lime.Protocol TCP transport
    /// </summary>
    public static class LimeTcpActivitySource
    {
        /// <summary>
        /// The name of the ActivitySource for the Lime.Protocol TCP transport
        /// </summary>
        public const string LimeTcp = "Lime.Protocol.Tcp";

        /// <summary>
        /// Gets the ActivitySource for the Lime.Protocol to be used when creating activities.
        /// </summary>
        public static readonly ActivitySource Instance = new ActivitySource(LimeTcp);
    }
}