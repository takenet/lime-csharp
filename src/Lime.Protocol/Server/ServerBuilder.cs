using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lime.Protocol.Listeners;

namespace Lime.Protocol.Server
{
    public class ServerBuilder
    {
        private readonly List<ITransportListener> _transportListeners;

        



    }

    public interface IServerBuilder
    {
        IServerBuilder AddTransportListener(ITransportListener transportListener);

        IServerBuilder WithChannelListener(IChannelListener channelListener);

        
    }

}
