using System.Diagnostics;

namespace Lime.Protocol.Tracing
{
    /// <summary>
    /// Provides the ActivitySource for the Lime.Protocol
    /// </summary>
    public static class LimeActivitySource
    {
        /// <summary>
        /// The name of the ActivitySource for the Lime.Protocol
        /// </summary>
        public const string LimeProtocol = "Lime.Protocol";

        /// <summary>
        /// Gets the ActivitySource for the Lime.Protocol to be used when creating activities.
        /// </summary>
        public static readonly ActivitySource Instance = new ActivitySource(LimeProtocol);
    }
}