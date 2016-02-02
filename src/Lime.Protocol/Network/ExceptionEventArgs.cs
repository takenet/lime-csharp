using Lime.Protocol.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Protocol.Network
{
    /// <summary>
    /// Provides informations about an event for an exception.
    /// </summary>
    public class ExceptionEventArgs : DeferralEventArgs
    {
        public ExceptionEventArgs(Exception exception)
        {
            if (exception == null) throw new ArgumentNullException(nameof(exception));            
            Exception = exception;
        }

        /// <summary>
        /// Exception related to the event.
        /// </summary>
        public Exception Exception { get; }
    }
}
