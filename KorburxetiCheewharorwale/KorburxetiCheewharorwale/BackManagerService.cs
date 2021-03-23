using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace KorburxetiCheewharorwale
{
    public class BackManagerService : BackgroundService
    {
        /// <inheritdoc />
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                Foo();
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }

        private void Foo()
        {
        }
    }
}