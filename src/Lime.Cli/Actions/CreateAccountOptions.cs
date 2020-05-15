using CommandLine;

namespace Lime.Cli.Actions
{
    [Verb("create-account", HelpText = "Creates an account in the server")]
    public class CreateAccountOptions
    {
        [Option(HelpText = "The account identity", Required = true)] 
        public string Identity { get; set; }
        
        [Option(HelpText = "The account password", Required = true)] 
        public string Password { get; set; }
        
        [Option(HelpText = "The account name")] 
        public string Name { get; set; }
    }
}