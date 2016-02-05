namespace Lime.Protocol.Listeners
{
    /// <summary>
    /// Defines a service that must be started to run its intended actions.
    /// </summary>
    public interface IStartable
    {
        /// <summary>
        /// Starts this instance.
        /// </summary>
        void Start();
    }
}
