using CommandLine;
using System.Collections.Generic;

namespace Lime.Cli.Actions
{
    [Verb("send-message", HelpText = "Sends a message to a node")]
    public class SendMessageOptions
    {
        [Option(HelpText = "The message id")]
        public string Id { get; set; }

        [Option(HelpText = "The message sender node")]
        public string From { get; set; }

        [Option(HelpText = "The message destination node", Required = true)]
        public string To { get; set; }

        [Option(HelpText = "The message destination node", Default = "text/plain")]
        public string Type { get; set; }

        [Option(HelpText = "The message content", Required = true, Separator = ' ')]
        public IEnumerable<string> Content { get; set; }

        public string JoinedContent => string.Join(' ', Content);
    }
}