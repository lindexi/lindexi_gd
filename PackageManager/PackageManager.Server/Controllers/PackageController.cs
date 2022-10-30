using Microsoft.AspNetCore.Mvc;

namespace PackageManager.Server.Controllers;

[ApiController]
[Route("[controller]")]
public class PackageController : ControllerBase
{
    [HttpGet]
    public string Get() => "Test";
}