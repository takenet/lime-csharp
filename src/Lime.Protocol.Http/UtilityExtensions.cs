using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Protocol.Http
{
    public static class UtilityExtensions
    {
        /// <summary>
        /// Extracts an variable value
        /// from the header or the query string.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="headerKey"></param>
        /// <param name="queryStringKey"></param>
        /// <returns></returns>
        public static string GetValue(this HttpListenerRequest request, string headerKey, string queryStringKey)
        {
            return request.Headers.Get(headerKey) ?? request.QueryString.Get(queryStringKey);
        }

        /// <summary>
        /// Gets the root path from a HTTP Listener request
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public static string GetRootPath(this Uri uri)
        {
            var segments = uri.Segments;

            if (segments.Length > 1)
            {
                return uri.Segments[1].TrimEnd('/').ToLowerInvariant();
            }
            else
            {
                return "/";
            }            
        }


        public static bool TryGetEnvelopeId(this Uri uri, out Guid messageId)
        {
            var segments = uri.Segments;
            if (segments.Length >= 3)
            {
                return Guid.TryParse(segments[2].TrimEnd('/'), out messageId);
            }

            messageId = default(Guid);
            return false;

        }

        public static HttpStatusCode ToHttpStatusCode(this Reason reason)
        {
            if (reason.Code >= 20 && reason.Code < 30)
            {
                // Validation errors
                return HttpStatusCode.BadRequest;
            }
            else if ((reason.Code >= 10 && reason.Code < 20) || (reason.Code >= 30 && reason.Code < 40))
            {
                // Session or Authorization errors
                return HttpStatusCode.Unauthorized;
            }

            return HttpStatusCode.Forbidden;
        }


        public static void SendResponse(this HttpListenerResponse response, HttpStatusCode statusCode, IDictionary<string, string> headers = null)
        {
            if (headers != null)
            {
                response.WriteHeaders(headers);
            }

            response.StatusCode = (int)statusCode;
            response.Close();
        }

        public static async Task SendResponseAsync(this HttpListenerResponse response, HttpStatusCode statusCode, string contentType, string content, IDictionary<string, string> headers = null)
        {
            if (headers != null)
            {
                response.WriteHeaders(headers);
            }

            response.StatusCode = (int)statusCode;
            response.ContentType = contentType;            
            using (var writer = new StreamWriter(response.OutputStream))
            {
                await writer.WriteAsync(content).ConfigureAwait(false);
            }
            response.Close();
        }

        public static void SendResponse(this HttpListenerResponse response, Reason reason, IDictionary<string, string> headers = null)
        {
            if (headers != null)
            {
                response.WriteHeaders(headers);
            }

            response.Headers.Add(Constants.REASON_CODE_HEADER, reason.Code.ToString());
            response.StatusCode = (int)reason.ToHttpStatusCode();
            response.StatusDescription = reason.Description;
            response.Close();
        }

        public static void WriteHeaders(this HttpListenerResponse response, IDictionary<string, string> headers)
        {
            foreach (var header in headers)
            {
                response.Headers.Add(header.Key, header.Value);
            }
        }
    }
}
