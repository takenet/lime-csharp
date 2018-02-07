using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Lime.Messaging.Contents;
using Lime.Messaging.Resources;
using Lime.Protocol;
using Lime.Protocol.Network;
using Lime.Protocol.Serialization;
using Lime.Protocol.Serialization.Newtonsoft;
using Lime.Protocol.UnitTests;
using Moq;
using NUnit.Framework;
using Shouldly;
using Lime.Messaging;
using NUnit.Framework;

namespace Lime.Transport.Redis.UnitTests
{
    public class RedisFixture : IDisposable
    {
        public RedisFixture()
        {
            var redisProcesses = Process.GetProcessesByName("redis-server");
            if (!redisProcesses.Any())
            {
                throw new Exception("Redis is not running");
            }
        }

        public Process RedisProccess { get; }

        public static string AssemblyDirectory
        {
            get
            {
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }

        public void Dispose()
        {
            RedisProccess?.Close();
            RedisProccess?.Dispose();
        }
    }
}
