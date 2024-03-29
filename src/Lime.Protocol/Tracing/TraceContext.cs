namespace Lime.Protocol.Tracing
{
    /// <summary>
    /// Defines the Trace Context constants.
    /// </summary>
    public static class TraceContext
    {
        /// <summary>
        /// The trace parent header name of the W3C trace context.
        /// </summary>
        public const string TraceParent = "traceparent";

        /// <summary>
        /// The trace state header name of the W3C trace context.
        /// </summary>
        public const string TraceState = "tracestate";
    }
}