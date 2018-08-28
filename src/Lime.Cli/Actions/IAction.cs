using Lime.Protocol.Network;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Lime.Cli.Actions
{
    public interface IAction
    {
        Type OptionsType { get; }

        Task ExecuteAsync(object options, IEstablishedChannel channel, CancellationToken cancellationToken);
    }
}
