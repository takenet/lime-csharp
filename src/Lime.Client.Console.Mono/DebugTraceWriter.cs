using Lime.Protocol.Network;
using Lime.Protocol.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Lime.Client.Console.Mono
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
