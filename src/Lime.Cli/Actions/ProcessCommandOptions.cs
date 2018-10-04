using CommandLine;
using Lime.Protocol;

namespace Lime.Cli.Actions
{
    [Verb("process-command", HelpText = "Process a command to a node and validates the response")]
    public class ProcessCommandOptions : SendCommandOptions
    {
        [Option(HelpText = "The expected command status", Default = CommandStatus.Success)]
        public CommandStatus ExpectedStatus { get; set; }

        [Option(HelpText = "A regex with the expected command resource in case of 'get' method", Default = CommandStatus.Success)]
        public string ExpectedResource { get; set; }

        [Option(HelpText = "The timeout for awaiting the response, in seconds", Default = 30)]
        public int Timeout { get; set; }
    }
}