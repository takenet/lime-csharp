using Lime.Protocol;

namespace Lime.Transport.AspNetCore.Transport
{
    /// <summary>
    /// Provides an instance of a registered channel.  
    /// </summary>
    public interface IChannelProvider
    {
        ISenderChannel? GetChannel(Node node);
        void AddChannel(Node node, SenderChannelAdapter senderChannel);
        void RemoveChannel(Node node);
    }
}