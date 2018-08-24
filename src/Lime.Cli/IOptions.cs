namespace Lime.Cli
{
    public interface IOptions : IConnectionOptions
    {
        bool Interactive { get; }
    }
}