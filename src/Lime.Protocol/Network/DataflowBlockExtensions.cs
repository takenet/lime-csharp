using System.Threading.Tasks.Dataflow;

namespace Lime.Protocol.Network
{
    public static class DataflowBlockExtensions
    {
        public static void CompleteIfNotCompleted(this IDataflowBlock dataflowBlock)
        {
            if (!dataflowBlock.Completion.IsCompleted) dataflowBlock.Complete();
        }
    }
}