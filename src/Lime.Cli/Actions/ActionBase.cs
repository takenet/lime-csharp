using Lime.Protocol.Network;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Lime.Cli.Actions
{
    public abstract class ActionBase<TOptions> : IAction
    {
        public Type OptionsType => typeof(TOptions);

        Task IAction.ExecuteAsync(object options, IEstablishedChannel channel, CancellationToken cancellationToken)
        {
            return ExecuteAsync((TOptions)options, channel, cancellationToken);
        }

        protected abstract Task ExecuteAsync(TOptions options, IEstablishedChannel channel, CancellationToken cancellationToken);
    }
}
