using Lime.Protocol.Client;
using Lime.Protocol.Network;
using Lime.Protocol.Server;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Protocol.UnitTests
{
    public static class SetupExtensions
    {

        public static async Task SetStateAsync(this IServerChannel channel, SessionState state)
        {
            //if (state >= SessionState.Authenticating)
            //{

            //    // Sets the state to Authenticating
            //    var schemeOptions = DataUtil.CreateSchemeOptions();
            //    await channel.SendAuthenticatingSessionAsync(schemeOptions);
            //}

            //if (state >= SessionState.Established)
            //{
            //    // Sets the state to Established
            //    var node = DataUtil.CreateNode();
            //    var mode = SessionMode.Server;
            //    await channel.SendEstablishedSessionAsync(node, mode);
            //}
        }
    }
}
