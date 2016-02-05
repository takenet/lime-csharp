namespace Lime.Protocol.Listeners
{
    /// <summary>
    /// Defines a service that must be stopped to not run anymore of its intended actions. 
    /// </summary>
    public interface IStoppable
    {
        /// <summary>
        /// Stops this instance.
        /// </summary>
        void Stop();
    }
}