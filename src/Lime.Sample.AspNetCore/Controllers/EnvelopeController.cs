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
    public class EnvelopeController : ControllerBase
    {
        private readonly ILogger<EnvelopeController> _logger;

        public EnvelopeController(ILogger<EnvelopeController> logger)
        {
            _logger = logger;
        }

        [HttpPost]
        [Route("/messages")]
        public IActionResult OnMessage(Message message, CancellationToken cancellationToken)
        {
            return Ok();
        }
        
        [HttpPost]
        [Route("/notification")]
        public IActionResult OnNotification(Notification notification, CancellationToken cancellationToken)
        {
            return Ok();
        }
        
        [HttpPost]
        [Route("/commands")]
        public IActionResult OnCommand(Command command, CancellationToken cancellationToken)
        {
            return Ok();
        }
    }
}