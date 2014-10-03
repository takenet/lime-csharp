using System;
using System.Collections.Generic;
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

        public HttpRequest(string method, Uri uri, IPrincipal user, Guid correlatorId = default(Guid), WebHeaderCollection headers = null, Stream body = null)
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

            if (correlatorId == default(Guid))
            {
                correlatorId = Guid.NewGuid();
            }

            Headers = headers;
            Body = body;
        }

        #endregion

        #region Public Properties

        public Guid CorrelatorId { get; private set; }

        public string Method { get; private set; }

        public Uri Uri { get; private set; }

        public IPrincipal User { get; private set; }

        public WebHeaderCollection Headers { get; private set; }

        public Stream Body { get; private set; }

        #endregion
    }
}
