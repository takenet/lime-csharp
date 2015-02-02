using System.Diagnostics;
using System.Threading.Tasks;
using Lime.Protocol.Network;

namespace Lime.Sample.Client
{
    public class DebugTraceWriter : ITraceWriter
    {
        private string _id;

        public DebugTraceWriter(string id)
        {
            _id = id;
        }

        #region ITraceWriter Members

        public Task TraceAsync(string data, DataOperation operation)
        {
            Debug.WriteLine("Id: {0} - Operation: {1} - Data: {2}", _id, operation, data);
            return Task.FromResult<object>(null);
        }

        public bool IsEnabled
        {
            get { return true; }
        }

        #endregion
    }
}
