using System;
using Microsoft.AspNetCore.SignalR;

namespace Lime.Transport.SignalR
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1812: Remove internal classes that are never instantiated", Justification = "The class is currently used in the Asp.Net DI container.")]
    internal class RandomUserIdProvider : IUserIdProvider
    {
        public string GetUserId(HubConnectionContext connection)
        {
            return Guid.NewGuid().ToString();
        }
    }

}
