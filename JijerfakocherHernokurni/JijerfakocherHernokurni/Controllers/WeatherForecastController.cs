using Microsoft.AspNetCore.Mvc;

namespace JijerfakocherHernokurni.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
    public IActionResult Post([FromBody] FooRequest request)
    {
        return Ok();
    }
}

public record FooRequest(string Name, string Version);
public record F1Request(string Name);