using Lime.Protocol;

namespace Lime.Transport.AspNetCore
{
    /// <summary>
    /// Defines a service for receiving <see cref="Message"/> envelopes.
    /// </summary>
    public interface IMessageListener : IEnvelopeListener<Message>
    {
        
    }
}