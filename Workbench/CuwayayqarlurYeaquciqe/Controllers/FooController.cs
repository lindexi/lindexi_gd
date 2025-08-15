using Microsoft.AspNetCore.Mvc;

namespace CuwayayqarlurYeaquciqe.Controllers;

[ApiController]
public class FooController : ControllerBase
{
    [HttpGet(template: "/")]
    [Route("/")]
    public string Get()
    {
        return "123123";
    }
}