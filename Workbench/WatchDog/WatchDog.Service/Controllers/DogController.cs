using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis.Extensions.Core.Implementations;

using WatchDog.Core;
using WatchDog.Service.Contexts;
using WatchDog.Service.Frameworks;

namespace WatchDog.Service.Controllers;

[ApiController]
[Route("[controller]")]
public class DogController : ControllerBase
{
    public DogController(ILogger<DogController> logger, IMasterHostProvider masterHostProvider,IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _masterHostProvider = masterHostProvider;
        _httpClientFactory = httpClientFactory;
    }

    private readonly ILogger<DogController> _logger;

    private readonly IMasterHostProvider _masterHostProvider;
    private readonly IHttpClientFactory _httpClientFactory;

    [HttpGet]
    [Route("Enable")]
    public string GetEnable()
    {
        return "Enable";
    }

    [HttpPost]
    [Route("Feed")]
    public async Task<FeedDogResponse?> FeedAsync(FeedDogRequest request)
    {
        _logger.LogInformation($"[Feed] {request.FeedDogInfo}");

        var host = await GetMasterHostAsync();
        var url = $"{host}WatchDog/Feed";
        var httpClient = _httpClientFactory.CreateClient();
        var response = await httpClient.PostAsJsonAsync(url, request);
        var result = await response.Content.ReadFromJsonAsync<FeedDogResponse>();
        return result;
    }

    [HttpPost]
    [Route("Wang")]
    public async Task<GetWangResponse?> GetWangAsync(GetWangRequest request, [FromServices] WatchDogProvider watchDogProvider)
    {
        var host = await GetMasterHostAsync();
        var url = $"{host}WatchDog/Wang";
        var httpClient = _httpClientFactory.CreateClient();
        var response = await httpClient.PostAsJsonAsync(url, request);
        var result = await response.Content.ReadFromJsonAsync<GetWangResponse>();

        if (result?.GetWangResult is {} wangResult)
        {
            lock (watchDogProvider)
            {
                watchDogProvider.Sync(wangResult);
            }
        }

        return result;
    }

    [HttpPost]
    [Route("Mute")]
    public async Task<MuteResponse?> MuteAsync(MuteRequest request)
    {
        var host = await GetMasterHostAsync();
        var url = $"{host}WatchDog/Mute";
        var httpClient = _httpClientFactory.CreateClient();
        var response = await httpClient.PostAsJsonAsync(url, request);
        var result = await response.Content.ReadFromJsonAsync<MuteResponse>();
        return result;
    }

    private async Task<string> GetMasterHostAsync()
    {
        var result = await _masterHostProvider.GetMasterHostAsync();
        var masterHost = result.MasterHost;

        if (masterHost.StartsWith("Fake-") || result.SelfIsMaster)
        {
            return "http://127.0.0.1:57725/";
        }
        else
        {
            return $"http://{masterHost}:57725/";
        }

    }
}

//public class MasterHostMiddleware
//{
//    public MasterHostMiddleware(RequestDelegate next)
//    {
//        _next = next;
//    }

//    private readonly RequestDelegate _next;

//    public async Task InvokeAsync(HttpContext context)
//    {

//    }
//}