using CommandLine;
using System.Collections.Generic;

namespace Lime.Cli.Actions
{
    [Verb("send-message", HelpText = "Sends a message to a node")]
    public class SendMessageOptions : SendEnvelopeOptions
    {
        [Option(HelpText = "The message content", Required = true, Separator = ' ')]
        public IEnumerable<string> Content { get; set; }

        public string JoinedContent => string.Join(' ', Content);
    }
}