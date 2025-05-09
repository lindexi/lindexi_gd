using Uno.UI.Hosting;
using WatchDog.Uno;

var host = UnoPlatformHostBuilder.Create()
    .App(() => new App())
    .UseWebAssembly()
    .Build();

await host.RunAsync();
