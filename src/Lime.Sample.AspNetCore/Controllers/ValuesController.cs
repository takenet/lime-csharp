using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Lime.Sample.AspNetCore.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ValuesController : ControllerBase
    {
        [HttpGet]
        [Route("/")]
        public IEnumerable<string> GetValues()
        {
            return new[]
            {
                "value1",
                "value2",
                "value3",
            };
        }
    }
}