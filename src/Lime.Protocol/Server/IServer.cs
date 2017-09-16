namespace Lime.Protocol.Server
{
    /// <summary>
    /// Defines a basic server infrastructure.
    /// </summary>
    public interface IServer : IStartable, IStoppable
    {
        /// <summary>
        /// Gets an established channel for the provided remote node.
        /// </summary>
        /// <param name="remoteNode"></param>
        /// <returns></returns>
        IServerChannel GetChannel(Node remoteNode);
    }
}