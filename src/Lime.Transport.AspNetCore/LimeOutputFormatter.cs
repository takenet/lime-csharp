using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace Lime.Transport.AspNetCore
{
    public class LimeOutputFormatter : OutputFormatter
    {
        public override Task WriteResponseBodyAsync(OutputFormatterWriteContext context)
        {
            throw new System.NotImplementedException();
        }
    }
}