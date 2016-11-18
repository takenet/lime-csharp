using System.Threading.Tasks;
using StackExchange.Redis;

namespace Lime.Transport.Redis
{
    public interface IConnectionMultiplexerFactory
    {
        Task<IConnectionMultiplexer> CreateAsync(ConfigurationOptions configurationOptions);
    }
}