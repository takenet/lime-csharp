using System.Diagnostics;
using System.Threading.Tasks;
using Lime.Protocol.Util;

namespace Lime.Protocol.Network
{
    /// <summary>
    /// Traces into the default .NET tracers.
    /// </summary>
    public sealed class TraceWriter : ITraceWriter
    {
        public Task TraceAsync(string data, DataOperation operation)
        {
            Trace.WriteLine(data, operation.ToString());
            return TaskUtil.CompletedTask;
        }

        public bool IsEnabled => Trace.Listeners.Count > 0;
    }
}
