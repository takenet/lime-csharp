using System.IO;
using System.Threading.Tasks;
using Lime.Protocol;
using Lime.Protocol.Serialization;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;

namespace Lime.Transport.AspNetCore
{
    public class EnvelopeInputFormatter : InputFormatter
    {
        private readonly IEnvelopeSerializer _envelopeSerializer;

        public EnvelopeInputFormatter(IEnvelopeSerializer envelopeSerializer)  
        {
            _envelopeSerializer = envelopeSerializer;
            SupportedMediaTypes.Clear();
            SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/json"));  
        }

        public override bool CanRead(InputFormatterContext context)
        {
            return !context.ModelType.IsAbstract && 
                   typeof(Envelope).IsAssignableFrom(context.ModelType);
        }

        public override async Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context)
        {
            using var reader = new StreamReader(context.HttpContext.Request.Body);
            var json = await reader.ReadToEndAsync();

            var envelope = _envelopeSerializer.Deserialize(json);
            
            return await InputFormatterResult.SuccessAsync(envelope);
        }
    }
}