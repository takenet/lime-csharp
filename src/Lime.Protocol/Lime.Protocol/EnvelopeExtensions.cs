namespace Lime.Protocol
{
    /// <summary>
    /// Utility extension methods for the <see cref="Envelope"/> class.
    /// </summary>
    public static class EnvelopeExtensions
    {
        /// <summary>
        /// Gets a shallow copy of the current envelope.
        /// </summary>
        /// <typeparam name="TEnvelope"></typeparam>
        /// <returns></returns>
        public static TEnvelope ShallowCopy<TEnvelope>(this TEnvelope envelope) where TEnvelope : Envelope, new()
        {
            return (TEnvelope)envelope.MemberwiseClone();
        }
    }
}