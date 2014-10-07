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

        public HttpRequest(string method, Uri uri, IPrincipal user, Guid correlatorId = default(Guid), WebHeaderCollection headers = null, NameValueCollection queryString = null, Stream bodyStream = null)
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

            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            User = user;

            if (correlatorId.Equals(default(Guid)))
            {
                CorrelatorId = Guid.NewGuid();
            }
            else
            {
                CorrelatorId = correlatorId;
            }

            if (headers != null)
            {
                Headers = headers;
            }
            else
            {
                Headers = new WebHeaderCollection();
            }

            if (queryString != null)
            {
                QueryString = queryString;
            }
            else
            {
                QueryString = new NameValueCollection();
            }

            BodyStream = bodyStream;
        }

        #endregion

        #region Public Properties

        public Guid CorrelatorId { get; private set; }

        public string Method { get; private set; }

        public Uri Uri { get; private set; }

        public IPrincipal User { get; private set; }

        public WebHeaderCollection Headers { get; private set; }

        public NameValueCollection QueryString { get; private set; }

        public Stream BodyStream { get; private set; }

        #endregion
    }
}
