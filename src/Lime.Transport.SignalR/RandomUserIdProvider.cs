using System;
using Microsoft.AspNetCore.SignalR;

namespace Lime.Transport.SignalR
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1812: Remove internal classes that are never instantiated", Justification = "The class is instantiated via reflection by ASP.NET")]
    internal class RandomUserIdProvider : IUserIdProvider
    {
        public string GetUserId(HubConnectionContext connection)
        {
            return Guid.NewGuid().ToString();
        }
    }

}
