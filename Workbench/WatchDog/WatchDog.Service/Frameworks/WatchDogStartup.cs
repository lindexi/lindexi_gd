using WatchDog.Core;
using WatchDog.Core.Services;

namespace WatchDog.Service.Frameworks;

public static class WatchDogStartup
{
    public static WebApplicationBuilder AddWatchDog(this WebApplicationBuilder builder)
    {
        var services = builder.Services;
        services.AddSingleton<ISelfHostProvider, FakeSelfHostProvider>();
        services.AddSingleton<IMasterHostProvider, RedisMasterHostProvider>();
        services.AddSingleton<IMasterHostStatusChecker, MasterHostStatusChecker>();

        services.AddSingleton<IDogInfoProvider, DogInfoProvider>();
        services.AddSingleton<ITimeProvider, TimeProvider>();
        services.AddSingleton<WatchDogProvider>();
        return builder;
    }

    public static WebApplication UseWatchDog(this WebApplication webApplication)
    {
        //webApplication.UseMiddleware<MasterHostMiddleware>();
        return webApplication;
    }
}