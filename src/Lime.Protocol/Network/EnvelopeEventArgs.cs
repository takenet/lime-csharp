using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Protocol.Network
{
    /// <summary>
    /// Holds the information for a envelope related event.
    /// </summary>
    public class EnvelopeEventArgs<T> : DeferralEventArgs, IEnvelopeContainer<T> where T : Envelope
    {
        public EnvelopeEventArgs(T envelope)
        {
            if (envelope == null) throw new ArgumentNullException(nameof(envelope));            
            Envelope = envelope;
        }

        /// <summary>
        /// The envelope related to the event.
        /// </summary>
        public T Envelope { get; }
    }
}
