using Lime.Protocol.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Protocol.Network
{
    /// <summary>
    /// Provides a command-style event,
    /// where the event generator will
    /// only continue the execution after
    /// the all the associated handlers 
    /// finishes they execution.
    /// <a href="http://blog.stephencleary.com/2013/02/async-oop-5-events.html"/>
    /// </summary>
    public class DeferralEventArgs : EventArgs
    {
        private readonly DeferralManager _deferrals;

        public DeferralEventArgs()
        {
            _deferrals = new DeferralManager();
        }

        public IDisposable GetDeferral()
        {
            return _deferrals.GetDeferral();
        }

        public Task WaitForDeferralsAsync()
        {
            return _deferrals.SignalAndWaitAsync();
        }
    }
}
