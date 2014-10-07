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

        public HttpResponse(Guid correlatorId, HttpStatusCode statusCode, string statusDescription = null, WebHeaderCollection headers = null, string body = null, string contentType = null)
        {
            if (correlatorId.Equals(default(Guid)))
            {
                throw new ArgumentException("CorrelatorId must be a valid GUID", "correlatorId");
            }

            CorrelatorId = correlatorId;

            StatusCode = statusCode;
            StatusDescription = statusDescription;
            if (headers != null)
            {
                Headers = headers;
            }
            else
            {
                Headers = new WebHeaderCollection();
            }

            Body = body;

            if (body != null)
            {
                if (contentType != null)
                {
                    Headers.Add(HttpResponseHeader.ContentType, contentType);
                }
                else
                {
                    Headers.Add(HttpResponseHeader.ContentType, Constants.TEXT_PLAIN_HEADER_VALUE);
                }
            }            
        }

        #endregion

        #region Public Properties

        public Guid CorrelatorId { get; private set; }

        public HttpStatusCode StatusCode { get; private set; }

        public string StatusDescription { get; private set; }

        public WebHeaderCollection Headers { get; private set; }

        public string Body { get; private set; }

        #endregion
    }    
}
