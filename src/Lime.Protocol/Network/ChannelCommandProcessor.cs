using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Lime.Protocol.Network
{
    /// <summary>
    /// Utility class for handling the command processing workflow for channels.
    /// </summary>
    /// <seealso cref="Lime.Protocol.Network.ICommandProcessor" />
    public sealed class ChannelCommandProcessor : IChannelCommandProcessor
    {
        private readonly ICommandSenderChannel _commandSenderChannel;
        private readonly ConcurrentDictionary<string, TaskCompletionSource<Command>> _pendingCommandsDictionary;

        public ChannelCommandProcessor(ICommandSenderChannel commandSenderChannel)
        {
            _commandSenderChannel = commandSenderChannel ?? throw new ArgumentNullException(nameof(commandSenderChannel));
            _pendingCommandsDictionary = new ConcurrentDictionary<string, TaskCompletionSource<Command>>();
        }
        
        public async Task<Command> ProcessCommandAsync(Command requestCommand, CancellationToken cancellationToken)
        {
            if (requestCommand == null) throw new ArgumentNullException(nameof(requestCommand));
            if (requestCommand.Status != CommandStatus.Pending)
            {
                throw new ArgumentException("Invalid command status", nameof(requestCommand));
            }

            if (requestCommand.Method == CommandMethod.Observe)
            {
                throw new ArgumentException("Invalid command method", nameof(requestCommand));
            }

            if (requestCommand.Id.IsNullOrEmpty())
            {
                throw new ArgumentException("Invalid command id", nameof(requestCommand));
            }

#if NETSTANDARD1_1
            // TODO: Without the TaskCreationOptions.RunContinuationsAsynchronously, there may occurs deadlock on command results.
            var tcs = new TaskCompletionSource<Command>();
#else
            var tcs = new TaskCompletionSource<Command>(TaskCreationOptions.RunContinuationsAsynchronously);
#endif

            if (!_pendingCommandsDictionary.TryAdd(requestCommand.Id, tcs))
            {
                throw new InvalidOperationException("Could not register the pending command request. The command id is already in use.");
            }
            try
            {
#if NETSTANDARD1_1
                using (cancellationToken.Register(() => tcs.TrySetCanceled()))
#else
                using (cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken)))
#endif
                {
                    await _commandSenderChannel.SendCommandAsync(requestCommand, cancellationToken).ConfigureAwait(false);
                    var result = await tcs.Task.ConfigureAwait(false);
                    return result;
                }
            }
            finally
            {
                TaskCompletionSource<Command> removedTcs;
                _pendingCommandsDictionary.TryRemove(requestCommand.Id, out removedTcs);
            }
        }

        public bool TrySubmitCommandResult(Command responseCommand)
        {
            if (responseCommand == null) throw new ArgumentNullException(nameof(responseCommand));
            TaskCompletionSource<Command> pendingRequestCommand;
            if (responseCommand.Id.IsNullOrEmpty() ||
                responseCommand.Status == CommandStatus.Pending ||
                responseCommand.Method == CommandMethod.Observe ||
                !_pendingCommandsDictionary.TryRemove(responseCommand.Id, out pendingRequestCommand) ||
                !pendingRequestCommand.TrySetResult(responseCommand))
            {
                return false;
            }

            return true;
        }

        public void CancelAll()
        {
            _pendingCommandsDictionary.Values.ToList().ForEach(tcs => tcs.TrySetCanceled());
            _pendingCommandsDictionary.Clear();
        }
    }
}