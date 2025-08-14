using CheejairkafeCayfelnoyikilur;

using Microsoft.AspNetCore.Mvc;

namespace CairkerkugelLerehenalcaceenel.Controllers;

[ApiController]
public class FooController : ControllerBase
{
    [HttpGet(template: "/")]
    public string Get()
    {
        return TestService.Greet;
    }
}