using System;
using System.Threading;
using System.Threading.Tasks;

namespace Lime.Protocol.Network.Modules
{
    public sealed class ChannelModule<T> : ChannelModuleBase<T> where T : Envelope, new()
    {
        private Func<T, CancellationToken, Task<T>> _onReceivingFunc;
        private Func<T, CancellationToken, Task<T>> _onSendingFunc;
        private Action<SessionState> _onStateChangedAction;

        public ChannelModule(Func<T, CancellationToken, Task<T>> onReceivingFunc, Func<T, CancellationToken, Task<T>> onSendingFunc, Action<SessionState> onStateChangedAction)
        {
            _onReceivingFunc = onReceivingFunc ?? throw new ArgumentNullException(nameof(onReceivingFunc));
            _onSendingFunc = onSendingFunc ?? throw new ArgumentNullException(nameof(onSendingFunc));
            _onStateChangedAction = onStateChangedAction ?? throw new ArgumentNullException(nameof(onStateChangedAction));
        }

        public override Task<T> OnReceivingAsync(T envelope, CancellationToken cancellationToken) 
            => _onReceivingFunc(envelope, cancellationToken);

        public override Task<T> OnSendingAsync(T envelope, CancellationToken cancellationToken) 
            => _onSendingFunc(envelope, cancellationToken);

        public override void OnStateChanged(SessionState state)
            => _onStateChangedAction(state);
    }
}