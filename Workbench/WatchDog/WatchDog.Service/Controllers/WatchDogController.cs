using Microsoft.AspNetCore.Mvc;
using WatchDog.Core;
using WatchDog.Service.Contexts;

namespace WatchDog.Service.Controllers;

[ApiController]
[Route("[controller]")]
public class WatchDogController : ControllerBase
{
    // 这是内部调用的
    public WatchDogController(WatchDogProvider watchDogProvider)
    {
        _watchDogProvider = watchDogProvider;
    }

    private readonly WatchDogProvider _watchDogProvider;

    [HttpPost]
    [Route("Feed")]
    public async Task<FeedDogResponse?> FeedAsync(FeedDogRequest request)
    {
        await Task.CompletedTask;

        lock (_watchDogProvider)
        {
            var result = _watchDogProvider.Feed(request.FeedDogInfo);
            return new FeedDogResponse(result);
        }
    }

    [HttpPost]
    [Route("Wang")]
    public async Task<GetWangResponse?> GetWangAsync(GetWangRequest request)
    {
        await Task.CompletedTask;
        lock (_watchDogProvider)
        {
            var result = _watchDogProvider.GetWang(request.GetWangInfo);
            return new GetWangResponse(result);
        }
    }

    [HttpPost]
    [Route("Mute")]
    public async Task<MuteResponse?> MuteAsync(MuteRequest request)
    {
        await Task.CompletedTask;
        lock (_watchDogProvider)
        {
            _watchDogProvider.Mute(request.MuteInfo);
            return new MuteResponse();
        }
    }
}