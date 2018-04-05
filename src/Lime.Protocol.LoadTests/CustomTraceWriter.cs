using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol.Network;
using Lime.Protocol.Serialization;
using Lime.Protocol.Serialization.Newtonsoft;
using Lime.Protocol.UnitTests;
using Lime.Transport.Tcp;
using Lime.Transport.WebSocket;
using Shouldly;
using Xunit;

namespace Lime.Protocol.LoadTests
{

    public sealed class CustomTraceWriter : ITraceWriter
    {
        public CustomTraceWriter()
        {
        }

        public Task TraceAsync(string data, DataOperation operation)
        {
            return Task.CompletedTask;
        }

        public bool IsEnabled => true;
    }
}
