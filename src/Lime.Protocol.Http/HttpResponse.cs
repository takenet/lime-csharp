using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace Lime.Protocol.Http
{
    /// <summary>
    /// Encapsulates a HTTP
    /// response message.
    /// </summary>
    public sealed class HttpResponse
    {
        #region Constructor

        public HttpResponse(Guid correlatorId, HttpStatusCode statusCode, string statusDescription = null, WebHeaderCollection headers = null, Stream body = null)
        {
            if (correlatorId == default(Guid))
            {
                throw new ArgumentException("The correlatorId must be a valid GUID", "correlatorId");
            }

            CorrelatorId = correlatorId;
            StatusCode = statusCode;
            StatusDescription = statusDescription;
            Headers = headers;
            Body = body;
        }

        #endregion

        #region Public Properties

        public Guid CorrelatorId { get; private set; }

        public HttpStatusCode StatusCode { get; private set; }

        public string StatusDescription { get; private set; }

        public WebHeaderCollection Headers { get; private set; }

        public Stream Body { get; private set; }

        #endregion
    }    
}
