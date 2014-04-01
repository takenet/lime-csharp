using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lime.Protocol.Client
{
    /// <summary>
    /// Helper extensions for the 
    /// IClientChannel interface
    /// </summary>
    public static class IClientChannelExtensions
    {
        public static Task<Session> EstablishSessionAsync(this IClientChannel channel, CancellationToken cancellationToken)
        {
            // TODO: 
            return Task.FromResult(new Session());
        }
    }
}
