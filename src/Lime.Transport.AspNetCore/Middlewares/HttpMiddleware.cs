using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Lime.Protocol;
using Lime.Protocol.Security;
using Lime.Protocol.Serialization;
using Lime.Transport.AspNetCore.Transport;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace Lime.Transport.AspNetCore.Middlewares
{
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    internal class HttpMiddleware
    {
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
        private readonly ILogger<HttpMiddleware> _logger;
        private readonly Dictionary<int, HttpEndPointOptions> _portEndPointOptions;

        public HttpMiddleware(
            RequestDelegate next,
            IEnvelopeSerializer envelopeSerializer,
            IOptions<LimeOptions> options,
            TransportListener transportListener,
            ILogger<HttpMiddleware> logger)
        {
            _next = next;
            _envelopeSerializer = envelopeSerializer;
            _options = options;
            _transportListener = transportListener;
            _logger = logger;
            _portEndPointOptions = options
                .Value
                .EndPoints.Where(e => e.Transport == TransportType.Http)
                .ToDictionary(e => e.EndPoint.Port,
                    e => e.Options as HttpEndPointOptions ?? new HttpEndPointOptions());
            ValidateOptions();
        }

        public async Task Invoke(HttpContext context)
        {
            if (context.Request.Method != HttpMethods.Post ||
                !EnvelopeContentTypes.Contains(context.Request.ContentType) ||
                !_portEndPointOptions.TryGetValue(context.Connection.LocalPort, out var endPointOptions) ||
                !endPointOptions.ContainsPath(context.Request.Path))
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

            var envelope = await ReadEnvelopeAsync(context);

            var channel = new HttpContextChannel(
                context,
                _options.Value.LocalNode,
                identity.ToNode(),
                _envelopeSerializer);
            
            await HandleEnvelopeAsync(envelope, channel, context);
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

            return result.DomainRole != DomainRole.Unknown 
                ? identity : 
                null;
        }

        private static (Identity, Authentication) GetAuthentication(AuthenticationHeaderValue header)
        {
            var identityAndSecret = header.Parameter.FromBase64().Split(':');
            if (identityAndSecret.Length != 2)
            {
                throw new ArgumentException("Invalid authentication parameter");
            }

            // For transport authentication, use the cert authentication from ASP.NET pipeline.
            // https://docs.microsoft.com/en-us/aspnet/core/security/authentication/certauth
            Identity identity = identityAndSecret[0];
            switch (header.Scheme.ToLowerInvariant())
            {
                case "basic":
                    return (identity, new PlainAuthentication()
                    {
                        Password = identityAndSecret[1].ToBase64()
                    });
                case "key":
                    return (identity, new KeyAuthentication()
                    {
                        Key = identityAndSecret[1].ToBase64()
                    });
                default:
                    throw new NotSupportedException($"Unsupported authentication scheme '{header.Scheme}'");
            }
        }
        
        private async Task<Envelope?> ReadEnvelopeAsync(HttpContext context)
        {
            using var reader = new StreamReader(context.Request.Body);
            var json = await reader.ReadToEndAsync();
            var envelope = _envelopeSerializer.Deserialize(json);
            return envelope;
        }
        
        private async Task HandleEnvelopeAsync(Envelope? envelope, HttpContextChannel channel, HttpContext context)
        {
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
        
        private void ValidateOptions()
        {
            foreach (var (port, option) in _portEndPointOptions)
            {
                if (!option.IsValid())
                {
                    throw new InvalidOperationException($"The HTTP configuration options value for port {port} is not valid");
                }
            }
        }
    }
}