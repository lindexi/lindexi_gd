using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.Extensions.Logging;
using WatchDagServer.Data;
using WatchDagServer.Model;

namespace WatchDagServer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DogController : ControllerBase
    {
        public DogController(WatchDagServerContext watchDagServerContext, ILogger<DogController> logger)
        {
            WatchDagServerContext = watchDagServerContext;
            Logger = logger;
        }

        private WatchDagServerContext WatchDagServerContext { get; }

        private ILogger Logger { get; }

        [HttpGet(nameof(FeedDog))]
        public void FeedDog()
        {
            // 只是喂狗
        }

        [HttpGet(nameof(RegisterWatch))]
        public string RegisterWatch([FromQuery] string token)
        {
            return $"注册成功，汪";
        }

        [HttpPost(nameof(RegisterWatch))]
        public string RegisterWatch([FromBody] RegisterRequest request,[FromServices] WatchDog watchDog)
        {
            var registerContext = WatchDagServerContext.RegisterContext.FirstOrDefault(temp => temp.Token == request.Token);

            if (registerContext == null)
            {
                // 恭喜你，是第一次注册，汪
                registerContext = new RegisterContext
                {
                    Token = request.Token,
                };
                WatchDagServerContext.RegisterContext.Add(registerContext);
            }
            // 更新一下数据
            registerContext.CurrentDelayCount = 0;
            registerContext.MaxDelayCount = request.MaxDelayCount;
            registerContext.DelaySecond = request.DelaySecond;
            registerContext.LastRegisterTime = DateTimeOffset.Now;

            WatchDagServerContext.SaveChanges();

            watchDog.AddWatch(registerContext);

            return $"注册成功，汪";
        }
    }
}