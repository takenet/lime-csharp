using System.Threading.Channels;

namespace Lime.Protocol.Util
{
    /// <summary>
    /// Util class for working with channels from the System.Threading.Channels namespace.
    /// </summary>
    public static class ChannelUtil
    {
        /// <summary>
        /// Creates a <see cref="Channel{T}"/> instance accordingly to the provided capacity.
        /// If greater than 0, the channel will be bounded; otherwise unbounded.
        /// </summary>
        public static Channel<T> CreateForCapacity<T>(
            int capacity,
            bool singleReader = false,
            bool singleWriter = false,
            bool allowSynchronousContinuations = false,
            BoundedChannelFullMode fullMode = BoundedChannelFullMode.Wait)
        {
            if (capacity > 0)
            {
                return Channel.CreateBounded<T>(new BoundedChannelOptions(capacity)
                {
                    SingleReader = singleReader,
                    SingleWriter = singleWriter,
                    AllowSynchronousContinuations = allowSynchronousContinuations,
                    FullMode = fullMode
                });
            }
            
            return Channel.CreateUnbounded<T>(new UnboundedChannelOptions()
            {
                SingleReader = singleReader,
                SingleWriter = singleWriter,
                AllowSynchronousContinuations = allowSynchronousContinuations 
            });
        }
    }
}