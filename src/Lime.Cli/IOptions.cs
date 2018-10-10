namespace Lime.Cli
{
    public interface IOptions : IConnectionOptions
    {
        int Timeout { get; }

        string Action { get; }
    }
}