using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Lime.Protocol;
using Lime.Protocol.Security;
using Lime.Protocol.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace Lime.Transport.AspNetCore
{
    internal class LimeHttpMiddleware
    {
        private const string MESSAGES_PATH = "/messages";
        private const string COMMANDS_PATH = "/commands";
        private const string NOTIFICATIONS_PATH = "/notifications";

        private static readonly string[] EnvelopePaths =
        {
            MESSAGES_PATH,
            COMMANDS_PATH,
            NOTIFICATIONS_PATH
        };

        private static readonly string[] EnvelopeContentTypes =
        {
            MediaType.ApplicationJson.ToString(),
            EnvelopeExtensions.MESSAGE_MIME_TYPE,
            EnvelopeExtensions.COMMAND_MIME_TYPE,
            EnvelopeExtensions.NOTIFICATION_MIME_TYPE,
        };

        private readonly RequestDelegate _next;
        private readonly IEnvelopeSerializer _envelopeSerializer;
        private readonly IOptions<LimeOptions> _options;
        private readonly TransportListener _transportListener;
        private readonly ILogger<LimeHttpMiddleware> _logger;
        private readonly int[] _httpPorts;

        public LimeHttpMiddleware(
            RequestDelegate next,
            IEnvelopeSerializer envelopeSerializer,
            IOptions<LimeOptions> options,
            TransportListener transportListener,
            ILogger<LimeHttpMiddleware> logger)
        {
            _next = next;
            _envelopeSerializer = envelopeSerializer;
            _options = options;
            _transportListener = transportListener;
            _logger = logger;
            _httpPorts = options
                .Value
                .EndPoints.Where(e => e.Transport == TransportType.Http)
                .Select(e => e.EndPoint.Port)
                .ToArray();
        }

        public async Task Invoke(HttpContext context)
        {
            if (!_httpPorts.Contains(context.Connection.LocalPort) ||
                context.Request.Method != HttpMethods.Post ||
                !EnvelopePaths.Any(e => string.Equals(e, context.Request.Path, StringComparison.OrdinalIgnoreCase)) ||
                !EnvelopeContentTypes.Contains(context.Request.ContentType))
            {
                await _next(context);
                return;
            }

            var identity = await AuthenticateAsync(context); 
            if (identity == null)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.CompleteAsync();
                return;
            }

            using var reader = new StreamReader(context.Request.Body);
            var json = await reader.ReadToEndAsync();
            var envelope = _envelopeSerializer.Deserialize(json);

            var channel = new HttpContextChannel(
                context,
                _options.Value.LocalNode,
                identity.ToNode(),
                _envelopeSerializer);
            
            switch (envelope)
            {
                case Message message:
                    await _transportListener.OnMessageAsync(message, channel, context.RequestAborted);
                    break;

                case Notification notification:
                    await _transportListener.OnNotificationAsync(notification, channel, context.RequestAborted);
                    break;
                
                case Command command:
                    await _transportListener.OnCommandAsync(command, channel, context.RequestAborted);
                    break;
                
                default:
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    break;
            }
        }

        private async Task<Identity?> AuthenticateAsync(HttpContext context)
        {
            Identity identity;
            Authentication authentication;

            if (context.Request.Headers.TryGetValue(HeaderNames.Authorization, out var headerValue) &&
                AuthenticationHeaderValue.TryParse(headerValue, out var header))
            {
                try
                {
                    (identity, authentication) = GetAuthentication(header);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Bad authentication format");
                    return null;
                }
            }
            else
            {
                identity = new Identity(Guid.NewGuid().ToString().ToLowerInvariant(), _options.Value.LocalNode.Domain);
                authentication = new GuestAuthentication();
            }

            var result = await _options.Value.AuthenticationHandler(identity, authentication, context.RequestAborted);

            if (result.DomainRole != DomainRole.Unknown)
            {
                return identity;
            }

            return null;
        }

        private (Identity, Authentication) GetAuthentication(AuthenticationHeaderValue header)
        {
            var identityAndSecret = header.Parameter.FromBase64().Split(':');
            if (identityAndSecret.Length != 2)
            {
                throw new ArgumentException("Invalid authentication parameter");
            }

            // For transport authentication, use the cert authentication from ASP.net
            // https://docs.microsoft.com/en-us/aspnet/core/security/authentication/certauth
            Identity identity = identityAndSecret[0];
            switch (header.Scheme.ToLowerInvariant())
            {
                case "basic":
                    return (identity, new PlainAuthentication()
                    {
                        Password = identityAndSecret[1]
                    });
                case "key":
                    return (identity, new KeyAuthentication()
                    {
                        Key = identityAndSecret[1]
                    });
                default:
                    throw new NotSupportedException($"Unsupported authentication scheme '{header.Scheme}'");
            }
        }
    }
}