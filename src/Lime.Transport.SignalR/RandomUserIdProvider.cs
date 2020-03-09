using System;
using Microsoft.AspNetCore.SignalR;

namespace Lime.Transport.SignalR
{
    internal class RandomUserIdProvider : IUserIdProvider
    {
        public string GetUserId(HubConnectionContext connection)
        {
            return Guid.NewGuid().ToString();
        }
    }

}
