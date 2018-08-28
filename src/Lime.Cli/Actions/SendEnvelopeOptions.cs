using CommandLine;

namespace Lime.Cli.Actions
{
    public class SendEnvelopeOptions
    {
        [Option(HelpText = "The envelope id")]
        public string Id { get; set; }

        [Option(HelpText = "The envelope sender node")]
        public string From { get; set; }

        [Option(HelpText = "The envelope destination node")]
        public string To { get; set; }

        [Option(HelpText = "The envelope destination node", Default = "text/plain")]
        public string Type { get; set; }

        [Option(HelpText = "The envelope metadata")]
        public string Metadata { get; set; }

    }
}