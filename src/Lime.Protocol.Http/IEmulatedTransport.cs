using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lime.Protocol.Http
{
    public interface IEmulatedTransport
    {
        Task<Session> AuthenticateAsync(CancellationToken cancellationToken);

        Task CloseAsync(CancellationToken cancellationToken);

        Task<bool> SubmitAsync(Envelope envelope, CancellationToken cancellationToken);

        Task<Notification> ProcessMessageAsync(Message message, CancellationToken cancellationToken);

        Task<Command> ProcessCommandAsync(Command command, CancellationToken cancellationToken);
    }
}
