using Lime.Protocol;

namespace Lime.Transport.AspNetCore
{
    /// <summary>
    /// Defines a service for receiving <see cref="Notification"/> envelopes.
    /// </summary>
    public interface INotificationListener : IEnvelopeListener<Notification>
    {
        
    }
}