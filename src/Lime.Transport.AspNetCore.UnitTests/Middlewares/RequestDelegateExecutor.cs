using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Lime.Transport.AspNetCore.UnitTests.Middlewares
{
    public class RequestDelegateExecutor
    {
        public RequestDelegateExecutor()
        {
            NextCallCount = 0;
            HttpContexts = new List<HttpContext>();
        }
        
        public int NextCallCount { get; private set; }
        
        public List<HttpContext> HttpContexts { get; }
        
        public Task Next(HttpContext context)
        {
            NextCallCount++;
            HttpContexts.Add(context);
            return Task.CompletedTask;
        }
    }
}