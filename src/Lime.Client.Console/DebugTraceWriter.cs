using Lime.Protocol.Network;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Client.Console
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
