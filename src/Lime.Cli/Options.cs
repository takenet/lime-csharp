
using CommandLine;
using System;

namespace Lime.Cli
{
    public class Options : IOptions
    {
        [Option(HelpText = "The identity for connection")]
        public string Identity { get; set; }

        [Option]
        public string Password { get; set; }

        [Option]
        public string AccessKey { get; set; }

        [Option(HelpText = "The session instance name")]
        public string Instance { get; set; }

        [Option(HelpText = "The address of the server to connect to", Required = true)]
        public Uri Uri { get; set; }

        [Option("presence.status")]
        public string PresenceStatus { get; set; }

        [Option("presence.routingrule")]
        public string PresenceRoutingRule { get; set; }

        [Option("receipt.events")]
        public string ReceiptEvents { get; set; }

        [Option(HelpText = "The timeout for channel operations, in seconds", Default = 30)]
        public int Timeout { get; set; }

        [Option(HelpText = "The action to be executed in the non-interactive mode")]
        public string Action { get; set; }
    }
}
