using Lime.Protocol;

namespace Lime.Transport.AspNetCore
{
    /// <summary>
    /// Defines a service for receiving <see cref="Command"/> envelopes.
    /// </summary>
    public interface ICommandListener : IEnvelopeListener<Command>
    {
        
    }
}