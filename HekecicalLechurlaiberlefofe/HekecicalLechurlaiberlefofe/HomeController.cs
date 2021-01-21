using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HekecicalLechurlaiberlefofe
{
    [ApiController]
    [Route("[controller]")]
    public class HomeController : ControllerBase
    {
        [HttpPost]
        public IActionResult Post([FromBody] FooRequest fooRequest)
        {
            return Ok(new FooResponse()
            {
                Name = fooRequest.Name
            });
        }
    }
}
