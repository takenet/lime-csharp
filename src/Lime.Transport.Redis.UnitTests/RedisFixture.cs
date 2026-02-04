using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

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
