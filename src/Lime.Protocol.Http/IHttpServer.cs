using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lime.Protocol.Http
{
    public interface IHttpServer
    {
        void Start();

        void Stop();
        
        Task<HttpRequest> AcceptRequestAsync(CancellationToken cancellationToken);

        Task SubmitResponseAsync(HttpResponse response, CancellationToken cancellationToken);
    }
}