using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Security.Principal;
using System.Text;

namespace Lime.Protocol.Http
{
    /// <summary>
    /// Encapsulates a HTTP
    /// request message.
    /// </summary>
    public sealed class HttpRequest
    {
        #region Constructor
        
        public HttpRequest(string method, Uri uri, IPrincipal user = null, Guid correlatorId = default(Guid), WebHeaderCollection headers = null, NameValueCollection queryString = null, MediaType contentType = null, Stream bodyStream = null)
        {
            if (string.IsNullOrWhiteSpace(method))
            {
                throw new ArgumentNullException("method");
            }
            Method = method;

            if (uri == null)
            {
                throw new ArgumentNullException("uri");
            }
            Uri = uri;

            User = user;
            CorrelatorId = correlatorId.Equals(default(Guid)) ? Guid.NewGuid() : correlatorId;
            Headers = headers ?? new WebHeaderCollection();
            QueryString = queryString ?? new NameValueCollection();

            if (bodyStream != null)
            {
                BodyStream = bodyStream;
                ContentType = contentType ?? MediaType.Parse(Constants.TEXT_PLAIN_HEADER_VALUE);
                Headers.Remove(HttpRequestHeader.ContentType);
                Headers.Add(HttpRequestHeader.ContentType, ContentType.ToString());                                
            }
        }

        #endregion

        #region Public Properties

        public Guid CorrelatorId { get; private set; }

        public string Method { get; private set; }

        public Uri Uri { get; private set; }

        public IPrincipal User { get; private set; }

        public WebHeaderCollection Headers { get; private set; }

        public NameValueCollection QueryString { get; private set; }

        public MediaType ContentType { get; private set; }

        public Stream BodyStream { get; private set; }

        #endregion
    }
}
