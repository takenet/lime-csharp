namespace Lime.Transport.AspNetCore
{
    /// <summary>
    /// The supported transport types. 
    /// </summary>
    public enum TransportType
    {
        /// <summary>
        /// Uses the TCP transport.
        /// </summary>
        Tcp,
        
        /// <summary>
        /// Uses the WebSocket transport.
        /// </summary>
        WebSocket,
        
        /// <summary>
        /// Uses the HTTP transport.
        /// </summary>
        Http
    }
}