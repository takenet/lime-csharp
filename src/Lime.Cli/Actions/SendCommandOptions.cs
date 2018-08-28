using CommandLine;
using Lime.Protocol;
using System.Collections.Generic;

namespace Lime.Cli.Actions
{
    [Verb("send-command", HelpText = "Sends a command to a node")]
    public class SendCommandOptions : SendEnvelopeOptions
    {
        [Option(HelpText = "The command URI", Required = true)]
        public string Uri { get; set; }

        [Option(HelpText = "The command method", Default = CommandMethod.Get)]
        public CommandMethod Method { get; set; }

        [Option(HelpText = "The command resource",  Separator = ' ')]
        public IEnumerable<string> Resource { get; set; }

        public string JoinedResource => Resource != null  ? string.Join(' ', Resource) : null;
    }
}