using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis.Extensions.Core.Abstractions;

namespace WatchDog.Service.Controllers;

[ApiController]
[Route("[controller]")]
public class DogController : ControllerBase
{
    private readonly ILogger<DogController> _logger;

    public DogController(ILogger<DogController> logger, IRedisClient redisClient)
    {
        _logger = logger;
    }
}