using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Protocol.Network
{
    /// <summary>
    /// Enable request tracing
    /// for network transports
    /// </summary>
    public interface ITraceWriter
    {
        /// <summary>
        /// Trace some data
        /// </summary>
        /// <param name="data"></param>
        /// <param name="operation"></param>
        /// <returns></returns>
        Task TraceAsync(string data, DataOperation operation);

        /// <summary>
        /// Indicates if the tracer
        /// is enabled
        /// </summary>
        bool IsEnabled { get; }
    }

    /// <summary>
    /// Represents the transport
    /// data operations
    /// </summary>
    public enum DataOperation
    {
        /// <summary>
        /// A transport send operation
        /// </summary>
        Send,
        /// <summary>
        /// A tranport receive operation
        /// </summary>
        Receive
    }
}
