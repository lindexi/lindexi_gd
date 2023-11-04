using Microsoft.Maui.Controls.Internals;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;

namespace DawfallbibolaiwaJidunawkefayhaiwere;

public static class MauiProgram
{
    static void Main(string[] args)
    {
        var mauiApp = CreateMauiApp();
        var rootContext = new MauiContext(mauiApp.Services);

        var app = mauiApp.Services.GetRequiredService<IApplication>();
        app.SetApplicationHandler(app, rootContext);

        var window = (Microsoft.Maui.Controls.Window) app.CreateWindow(null);
        window.Page = new MainPage();
        app.OpenWindow(window);
    }

    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Services.ConfigureMauiHandlers(collection =>
        {
            collection.AddHandler<Application, FooApplicationHandler>();
            collection.AddHandler<Microsoft.Maui.Controls.Window, FooWindowHandler>();
            collection.AddHandler<Page, FooPageHandler>();
        });

        // 这是一个标记过时的类型，只是 MAUI 为了兼容之前的坑而已，后续版本将会删除
        DependencyService.Register<ISystemResourcesProvider, ObsoleteSystemResourcesProvider>();

        return builder.Build();
    }
}

class ObsoleteSystemResourcesProvider : ISystemResourcesProvider
{
    public IResourceDictionary GetSystemResources()
    {
        return new ResourceDictionary();
    }
}

class FooPageHandler : PageHandler
{
    protected override object CreatePlatformView()
    {
        return new object();
    }
}

class FooWindowHandler : WindowHandler
{
    protected override object CreatePlatformElement()
    {
        return new object();
    }

    public override void UpdateValue(string property)
    {
        var window = (Microsoft.Maui.Controls.Window) VirtualView;
        if (property == "Page")
        {
            var page = window.Page;
            var platform = page.ToPlatform(MauiContext);
        }

        var mauiContext = this.MauiContext;

        base.UpdateValue(property);
    }
}

class FooApplicationHandler : ApplicationHandler
{
    protected override object CreatePlatformElement()
    {
        return new object();
    }
}