using System;
using System.Threading;

namespace Lime.Protocol.Network
{
    public static class CancellationTokenSourceExtensions
    {
        public static void CancelIfNotRequested(this CancellationTokenSource cts)
        {
            if (!cts.IsCancellationRequested) cts.Cancel();
        }

        public static void CancelAndDispose(this CancellationTokenSource cts)
        {
            try
            {
                cts.CancelIfNotRequested();
                cts.Dispose();
            }
            catch (ObjectDisposedException) {}
        }

        public static bool IsCancellationRequestedOrDisposed(this CancellationTokenSource cts)
        {
            if (cts.IsCancellationRequested) return true;

            try
            {
                _ = cts.Token;
            }
            catch (ObjectDisposedException)
            {
                return true;
            }
            
            return false;
        }
    }
}