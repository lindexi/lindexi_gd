using System.Windows.Controls;
using MauiApp;

using Microsoft.Maui.Hosting;
using MauiWpfAdapt.Handlers;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Controls.Internals;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using Button = Microsoft.Maui.Controls.Button;
using Page = Microsoft.Maui.Controls.Page;

namespace MauiWpfAdapt.Hosts;

public static class MauiForWpfHostHelper
{
    public static MauiApplicationProxy InitMauiApplication(System.Windows.Application wpfApplication)
    {
        var builder = MauiProgram.CreateMauiAppBuilder();

        builder.Services.ConfigureMauiHandlers(collection =>
        {
            collection.AddHandler<Application, FooApplicationHandler>();
            collection.AddHandler<Microsoft.Maui.Controls.Window, FooWindowHandler>();
            collection.AddHandler<Page, FooPageHandler>();
            collection.AddHandler<Layout, FooLayoutHandler>();
            collection.AddHandler<Button, FooButtonHandler>();
        });

        // 这是一个标记过时的类型，只是 MAUI 为了兼容之前的坑而已，后续版本将会删除
        DependencyService.Register<ISystemResourcesProvider, ObsoleteSystemResourcesProvider>();

        var mauiApp = builder.Build();

        var rootContext = new FooMauiContext(wpfApplication, mauiApp.Services);

        var app = mauiApp.Services.GetRequiredService<IApplication>();
        app.SetApplicationHandler(app, rootContext);
        var fooApplicationHandler = (FooApplicationHandler) app.Handler;
        _ = fooApplicationHandler;

        return new MauiApplicationProxy(app);
    }

    public static void HostMainPage(Panel panel, MauiApplicationProxy applicationProxy)
    {
        var application = applicationProxy.Application;
        var context = application.Handler!.MauiContext!;

        var mauiContext = new FooPanelMauiContext(panel, context);
        var mauiWindow = (Microsoft.Maui.Controls.Window) application.CreateWindow(new ActivationState(mauiContext));
        mauiWindow.SetWindowHandler(mauiWindow, mauiContext);
        mauiWindow.Width = panel.Width;
        mauiWindow.Height = panel.Height;

        var mainPage = new MainPage()
        {
            WidthRequest = panel.Width,
            HeightRequest = panel.Height,
        };
        var platform = mainPage.ToPlatform(mauiContext);
        _ = platform;

        mainPage.Measure(panel.Width, panel.Height);
        ////mainPage.Arrange(new Rect(0, 0, panel.Width, panel.Height));

        mainPage.Layout(new Rect(0, 0, panel.Width, panel.Height));

        //var verticalStackLayout = (VerticalStackLayout) mainPage.Content;
        ////verticalStackLayout.Measure(panel.Width, panel.Height);
        ////verticalStackLayout.Layout(new Rect(0, 0, panel.Width, panel.Height));

        //verticalStackLayout.CrossPlatformMeasure(panel.Width, panel.Height);
        //verticalStackLayout.CrossPlatformArrange(new Rect(0, 0, panel.Width, panel.Height));

        mauiWindow.Page = mainPage;
        //var windowManager = mauiContext.Services.GetRequiredService<NavigationRootManager>()

        //mauiWindow.VisualDiagnosticsOverlay.AddWindowElement(new RectangleAdorner(mainPage));
        var window = (IWindow) mauiWindow;
        window.FrameChanged(new Rect(0, 0, panel.Width, panel.Height));
        window.Created();
        window.Activated();
    }
}