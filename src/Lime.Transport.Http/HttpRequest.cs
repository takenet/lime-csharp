using System;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Security.Principal;
using Lime.Protocol;

namespace Lime.Transport.Http
{
    /// <summary>
    /// Encapsulates a HTTP
    /// request message.
    /// </summary>
    public sealed class HttpRequest
    {
        public HttpRequest(
            string method, 
            Uri uri, 
            IPrincipal user = null, 
            string correlatorId = null,
            WebHeaderCollection headers = null, 
            NameValueCollection queryString = null, 
            MediaType contentType = null, 
            Stream bodyStream = null)
        {
            if (string.IsNullOrWhiteSpace(method)) throw new ArgumentNullException(nameof(method));
            if (uri == null) throw new ArgumentNullException(nameof(uri));
            
            Uri = uri;
            Method = method;
            User = user;
            CorrelatorId = string.IsNullOrWhiteSpace(correlatorId) ? Guid.NewGuid().ToString() : correlatorId;
            Headers = headers ?? new WebHeaderCollection();
            QueryString = queryString ?? new NameValueCollection();

            if (bodyStream != null)
            {
                BodyStream = bodyStream;
                ContentType = contentType ?? MediaType.Parse(Constants.TEXT_PLAIN_HEADER_VALUE);

                var contentTypeValue = Headers[HttpRequestHeader.ContentType];
                if (contentTypeValue != null)
                {
                    if (!MediaType.Parse(contentTypeValue.Split(';')[0]).Equals(ContentType))
                    {
                        Headers.Remove(HttpRequestHeader.ContentType);
                        Headers.Add(HttpRequestHeader.ContentType, ContentType.ToString());
                    }
                }
                else
                {
                    Headers.Add(HttpRequestHeader.ContentType, ContentType.ToString());
                }
            }
        }

        public string CorrelatorId { get; private set; }

        public string Method { get; private set; }

        public Uri Uri { get; private set; }

        public IPrincipal User { get; private set; }

        public WebHeaderCollection Headers { get; private set; }

        public NameValueCollection QueryString { get; private set; }

        public MediaType ContentType { get; private set; }

        public Stream BodyStream { get; private set; }
    }
}
