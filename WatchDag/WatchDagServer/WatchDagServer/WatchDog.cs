using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WatchDagServer.Data;
using WatchDagServer.Model;

namespace WatchDagServer
{
    public class WatchDog
    {
        public WatchDog(IServiceProvider serviceProvider, ILogger<WatchDog> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger _logger;

        public async void AddWatch(RegisterContext registerContext)
        {
            var token = registerContext.Token;

            if (WatchContextList.TryGetValue(token, out var context))
            {
                context.TaskCompletionSource.TrySetResult();
            }

            var delaySecond = registerContext.DelaySecond;

            var delayTask = Task.Delay(TimeSpan.FromSeconds(delaySecond));
            var taskCompletionSource = new TaskCompletionSource();

            WatchContextList[token] = new WatchContext()
            {
                TaskCompletionSource = taskCompletionSource
            };

            await Task.WhenAny(delayTask, taskCompletionSource.Task);

            Check(token);
        }

        private void Check(string token)
        {
            // 判定一下时间
            using (var serviceScope = _serviceProvider.CreateScope())
            {
                var watchDagServerContext = serviceScope.ServiceProvider.GetService<WatchDagServerContext>();

                var registerContext = watchDagServerContext!.RegisterContext.FirstOrDefault(temp => temp.Token == token);
                if (registerContext == null)
                {
                    return;
                }

                // 如果当前时间大于最后注册时间的 DelayTime 那么加一
                var delayTime = TimeSpan.FromSeconds(registerContext.DelaySecond);
                if (DateTimeOffset.Now - registerContext.LastRegisterTime > delayTime)
                {
                    registerContext.CurrentDelayCount++;
                    watchDagServerContext.SaveChanges();

                    if (registerContext.CurrentDelayCount > registerContext.MaxDelayCount)
                    {
                        // 咬人
                        _logger.LogError($"{registerContext.Token}已经很久没喂狗了");
                    }
                }

                // 再次注册回去
                AddWatch(registerContext);
            }
        }

        private ConcurrentDictionary<string, WatchContext> WatchContextList { get; } = new ConcurrentDictionary<string, WatchContext>();

        private class WatchContext
        {
            public TaskCompletionSource TaskCompletionSource { get; set; }
        }
    }
}