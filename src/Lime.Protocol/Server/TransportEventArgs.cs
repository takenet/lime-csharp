using Lime.Protocol.Network;
using System;

namespace Lime.Protocol.Server
{
    public class TransportEventArgs : EventArgs
    {
        public TransportEventArgs(ITransport transport)
        {
            if (transport == null)
            {
                throw new ArgumentNullException("transport");
            }

            this.Transport = transport;
        }

        public ITransport Transport { get; private set; }
    }
}
