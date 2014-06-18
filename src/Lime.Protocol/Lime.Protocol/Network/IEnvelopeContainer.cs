using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Protocol.Network
{
    public interface IEnvelopeContainer<out T> where T : Envelope
    {
        T Envelope { get; }
    }
}
