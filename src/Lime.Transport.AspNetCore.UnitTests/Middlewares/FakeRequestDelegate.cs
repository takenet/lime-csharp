using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Lime.Transport.AspNetCore.UnitTests.Middlewares
{
    public class FakeRequestDelegate
    {
        public FakeRequestDelegate()
        {
            NextCalls = 0;
            HttpContexts = new List<HttpContext>();
        }
        
        public int NextCalls { get; private set; }
        
        public List<HttpContext> HttpContexts { get; }
        
        public Task Next(HttpContext context)
        {
            NextCalls++;
            HttpContexts.Add(context);
            return Task.CompletedTask;
        }
    }
}