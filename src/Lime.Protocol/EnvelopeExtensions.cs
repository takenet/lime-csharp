namespace Lime.Protocol
{
    /// <summary>
    /// Utility extension methods for the <see cref="Envelope"/> class.
    /// </summary>
    public static class EnvelopeExtensions
    {
        /// <summary>
        /// Gets a shallow copy of the current <see cref="Envelope"/>.
        /// </summary>
        /// <typeparam name="TEnvelope"></typeparam>
        /// <returns></returns>
        public static TEnvelope ShallowCopy<TEnvelope>(this TEnvelope envelope) where TEnvelope : Envelope, new()
        {
            return (TEnvelope)envelope.MemberwiseClone();
        }

        /// <summary>
        /// Gets the sender node of the envelope.
        /// </summary>
        /// <param name="envelope">The envelope.</param>
        /// <returns></returns>
        public static Node GetSender(this Envelope envelope)
        {
            return envelope.Pp ?? envelope.From;
        }
    }
}