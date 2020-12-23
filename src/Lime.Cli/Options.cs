
using CommandLine;
using Lime.Protocol.Security;
using System;

namespace Lime.Cli
{
    public class Options : IOptions
    {
        [Option(HelpText = "The identity for connection.", Required = true)]
        public string Identity { get; set; }

        [Option(HelpText = "The password for using plain authentication.")]
        public string Password { get; set; }

        [Option(HelpText = "The access key for using key authentication.")]
        public string Key { get; set; }

        [Option(HelpText = "The token to be used on External Authentication")]
        public string Token { get; set; }

        [Option(HelpText = "The issuer to be used on External Authentication", Default = "account.blip.ai")]
        public string Issuer { get; set; }

        [Option(HelpText = "The session instance name.")]
        public string Instance { get; set; }

        [Option(HelpText = "The address of the server to connect to.", Required = true)]
        public Uri Uri { get; set; }

        [Option("presence.status", HelpText = "The session presence status to be set when established.")]
        public string PresenceStatus { get; set; }

        [Option("presence.routingrule", HelpText = "The session presence routing rule to be set when established.")]
        public string PresenceRoutingRule { get; set; }

        [Option("receipt.events", HelpText = "The notification required receipts to be set when established.")]
        public string ReceiptEvents { get; set; }

        [Option(HelpText = "The timeout for channel operations, in seconds.", Default = 30)]
        public int Timeout { get; set; }

        [Option(HelpText = "The action to be executed in the non-interactive mode.")]
        public string Action { get; set; }

        [Option(HelpText = "Thumbprint for the X509 Certificate to use as Transport Authentication.")]
        public string CertificateThumbprint { get; set; }

        [Option(HelpText = "The DomainRole to be used on Transport Authentication.", Default = DomainRole.Authority)]
        public DomainRole DomainRole { get; set; }
    }
}
