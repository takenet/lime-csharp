using Lime.Protocol.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
