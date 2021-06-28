using Lime.Protocol;

namespace Lime.Transport.AspNetCore.Listeners
{
    /// <summary>
    /// Defines a service for receiving <see cref="Notification"/> envelopes.
    /// </summary>
    public interface INotificationListener : IEnvelopeListener<Notification>
    {
        
    }
}