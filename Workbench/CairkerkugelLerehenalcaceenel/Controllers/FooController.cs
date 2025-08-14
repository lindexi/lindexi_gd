using CheejairkafeCayfelnoyikilur;

using Microsoft.AspNetCore.Mvc;

namespace CairkerkugelLerehenalcaceenel.Controllers;

[ApiController]
public class FooController : ControllerBase
{
    [HttpGet(template: "/")]
    [Route("/")]
    public string Get()
    {
        return TestService.Greet;
    }
}