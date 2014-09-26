using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Protocol.Http.Storage
{
    public interface IMessageStorage
    {
        Task StoreMessageAsync(Message message);

        Task<IEnumerable<Message>> GetMessagesAsync(Identity owner);

        Task DeleteMessageAsync(Guid id);
    }
}
