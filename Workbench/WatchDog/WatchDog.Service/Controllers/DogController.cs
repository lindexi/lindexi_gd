using Microsoft.AspNetCore.Mvc;

namespace WatchDog.Service.Controllers;

[ApiController]
[Route("[controller]")]
public class DogController : ControllerBase
{
    private readonly ILogger<DogController> _logger;

    public DogController(ILogger<DogController> logger)
    {
        _logger = logger;
    }
}