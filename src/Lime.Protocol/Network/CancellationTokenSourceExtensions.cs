using System.Threading;

namespace Lime.Protocol.Network
{
    public static class CancellationTokenSourceExtensions
    {
        public static void CancelIfNotRequested(this CancellationTokenSource cts)
        {
            if (!cts.IsCancellationRequested) cts.Cancel();
        }
    }
}