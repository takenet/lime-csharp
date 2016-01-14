using System;
using System.Net;
using Lime.Protocol;

namespace Lime.Transport.Http
{
    public static class UtilExtensions
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
        /// Extracts an variable value
        /// from the header or the query string.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="headerKey"></param>
        /// <param name="queryStringKey"></param>
        /// <returns></returns>
        public static string GetValue(this HttpRequest request, string headerKey, string queryStringKey)
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

        /// <summary>
        /// Gets a related HTTP code
        /// for the specified reason.
        /// </summary>
        /// <param name="reason"></param>
        /// <returns></returns>
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

        public static bool IsSuccess(this HttpStatusCode httpStatusCode)
        {
            return (int)httpStatusCode >= 200 && (int)httpStatusCode < 300;

        }
    }
}
