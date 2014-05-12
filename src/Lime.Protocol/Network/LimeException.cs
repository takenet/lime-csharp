using Lime.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Protocol.Network
{
    public class LimeException : Exception
    {
        public LimeException(int reasonCode, string reasonDescription, Exception innerException = null)
            : this(new Reason() {  Code = reasonCode, Description = reasonDescription}, innerException)
        {

        }

        public LimeException(Reason reason, Exception innerException = null)
            : base(reason.Description, innerException)
        {
            if (reason == null)
            {
                throw new ArgumentNullException("reason");                
            }

            this.Reason = reason;
        }

        public Reason Reason { get; private set; }
    }
}
