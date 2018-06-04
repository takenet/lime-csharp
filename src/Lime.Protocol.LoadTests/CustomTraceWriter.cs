using Lime.Protocol.Network;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Protocol.LoadTests
{
    public class CustomTraceWriter : ITraceWriter
    {
        public bool IsEnabled => true;

        public Task TraceAsync(string data, DataOperation operation)
        {
            return Task.CompletedTask;
        }
    }
}
