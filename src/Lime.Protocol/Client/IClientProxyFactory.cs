using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lime.Protocol.Network;

namespace Lime.Protocol.Client
{
    public interface IClientProxyFactory
    {
        T CreateProxy<T>(IClientChannel clientChannel) where T : class;
    }

    class ClientProxyFactory : IClientProxyFactory
    {
        public T CreateProxy<T>(IClientChannel clientChannel) where T : class
        {

            throw new NotImplementedException();
        }
    }
}
