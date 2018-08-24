
using CommandLine;
using System;

namespace Lime.Cli
{
    public class Options : IOptions
    {
        [Option(HelpText = "Run in interactive mode", Default = true)]
        public bool Interactive { get; set; }

        [Option(HelpText = "The identity for connection")]
        public string Identity { get; set; }

        [Option]
        public string Password { get; set; }

        [Option]
        public string AccessKey { get; set; }

        [Option(HelpText = "The session instance name")]
        public string Instance { get; set; }

        [Option]
        public Uri ServerUri { get; set; }

        [Option("presence.status")]
        public string PresenceStatus { get; set; }

        [Option("presence.routingrule")]
        public string PresenceRoutingRule { get; set; }

        [Option("receipt.events")]
        public string ReceiptEvents { get; set; }
    }
}
