using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace Lime.Transport.Redis
{
    public class ConnectionMultiplexerFactory : IConnectionMultiplexerFactory
    {
        public async Task<IConnectionMultiplexer> CreateAsync(ConfigurationOptions configurationOptions)
        {
            var connectionMultiplexer = await ConnectionMultiplexer.ConnectAsync(configurationOptions).ConfigureAwait(false);
            connectionMultiplexer.PreserveAsyncOrder = false;
            return connectionMultiplexer;
        }
    }
}
