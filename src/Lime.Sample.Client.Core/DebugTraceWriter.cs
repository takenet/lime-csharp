using System.Diagnostics;
using System.Threading.Tasks;
using Lime.Protocol.Network;
using Lime.Protocol.Util;

namespace Lime.Sample.Client
{
    public class DebugTraceWriter : ITraceWriter
    {
        public Task TraceAsync(string data, DataOperation operation)
        {
            Debug.WriteLine($"{operation} - {data}");
            return TaskUtil.CompletedTask;
        }

        public bool IsEnabled
        {
            get { return true; }
        }
    }
}
