using Lime.Protocol;

namespace Lime.Transport.AspNetCore.Listeners
{
    /// <summary>
    /// Defines a service for receiving <see cref="Command"/> envelopes.
    /// </summary>
    public interface ICommandListener : IEnvelopeListener<Command>
    {
        
    }
}