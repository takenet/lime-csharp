using Lime.Client.TestConsole.ViewModels;
using Lime.Protocol;
using Lime.Protocol.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Client.TestConsole.Macros
{
    [Macro(Name = "Reply ping", Category = "Command", IsActiveByDefault = true)]
    public class ReplyPingMacro : IMacro
    {
        #region IMacro Members

        public async Task ProcessAsync(EnvelopeViewModel envelopeViewModel, SessionViewModel sessionViewModel)
        {
            if (envelopeViewModel == null)
            {
                throw new ArgumentNullException("envelopeViewModel");
            }

            if (sessionViewModel == null)
            {
                throw new ArgumentNullException("sessionViewModel");
            }

            var command = envelopeViewModel.Envelope as Command;

            if (command != null &&
                command.Status == CommandStatus.Pending &&
                command.Uri != null &&
                command.Uri.Path.Equals(UriTemplates.PING))
            {
                var commandResponse = new Command()
                {
                    Id = command.Id,
                    Status = CommandStatus.Success,
                    To = command.From,
                    Resource = new Ping()
                };

                var commandEnvelopeViewModel = new EnvelopeViewModel()
                {
                    Envelope = commandResponse
                };

                sessionViewModel.InputJson = commandEnvelopeViewModel.Json;

                if (sessionViewModel.SendCommand.CanExecute(null))
                {
                    await sessionViewModel.SendCommand.ExecuteAsync(null);
                }
            }
        }

        #endregion
    }
}
