using System.Threading.Tasks.Dataflow;

namespace Lime.Protocol.Network
{
    internal static class DataflowUtils
    {
        public static readonly DataflowLinkOptions PropagateCompletionLinkOptions = new DataflowLinkOptions()
        {
            PropagateCompletion = true,
        };
    }
}