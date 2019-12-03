using System.Threading.Tasks.Dataflow;

namespace Lime.Protocol.Network
{
    internal static class DataflowUtils
    {
        public static readonly DataflowLinkOptions PropagateCompletionLinkOptions = new DataflowLinkOptions()
        {
            PropagateCompletion = true,
        };

        public static readonly ExecutionDataflowBlockOptions UnboundedUnorderedExecutionDataflowBlockOptions =
            new ExecutionDataflowBlockOptions()
            {
                BoundedCapacity = DataflowBlockOptions.Unbounded,
                MaxDegreeOfParallelism = DataflowBlockOptions.Unbounded,
                EnsureOrdered = false
            };
    }
}