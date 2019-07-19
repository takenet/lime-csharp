namespace Lime.Protocol
{
    /// <summary>
    /// Represents an element of a network.
    /// </summary>
    public interface INode : IIdentity
    {
        /// <summary>
        /// The name of the instance used by the node to connect to the network.
        /// </summary>
        string Instance { get; }
    }
}