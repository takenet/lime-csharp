using Lime.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Protocol.Network
{
    /// <summary>
    /// Represents protocol errors ocurred 
    /// during the communication
    /// </summary>
    public class LimeException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LimeException"/> class.
        /// </summary>
        /// <param name="reasonCode">The reason code.</param>
        /// <param name="reasonDescription">The reason description.</param>
        /// <param name="innerException">The inner exception.</param>
        public LimeException(int reasonCode, string reasonDescription, Exception innerException = null)
            : this(new Reason {  Code = reasonCode, Description = reasonDescription}, innerException)
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LimeException"/> class.
        /// </summary>
        /// <param name="reason">The reason.</param>
        /// <param name="innerException">The inner exception.</param>
        /// <exception cref="System.ArgumentNullException">reason</exception>
        public LimeException(Reason reason, Exception innerException = null)
            : base(reason.Description, innerException)
        {
            this.Reason = reason;
        }

        /// <summary>
        /// Associated reason 
        /// for the failure
        /// </summary>
        public Reason Reason { get; private set; }
    }
}
