using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lime.Protocol.Util;

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
        /// Indicates if the tracer is enabled.
        /// </summary>
        bool IsEnabled { get; }
    }

    /// <summary>
    /// Represents the transport operations.
    /// </summary>
    public enum DataOperation
    {
        /// <summary>
        /// A transport send operation
        /// </summary>
        Send,
        /// <summary>
        /// A transport receive operation
        /// </summary>
        Receive,
        /// <summary>
        /// A transport failure
        /// </summary>
        Error,
        /// <summary>
        /// A transport information
        /// </summary>
        Information
    }

    public static class TraceWriterExtensions
    {
        public static Task TraceIfEnabledAsync(this ITraceWriter traceWriter, string data, DataOperation operation)
        {
            if (traceWriter == null || !traceWriter.IsEnabled) return TaskUtil.CompletedTask;
            return traceWriter.TraceAsync(data, operation);
        }
    }
}
