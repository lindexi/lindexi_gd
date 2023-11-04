using System.Diagnostics;
using MauiWpfAdapt.Hosts;
using Microsoft.Maui;
using Microsoft.Maui.Handlers;

namespace MauiWpfAdapt.Handlers;

class FooApplicationHandler : ApplicationHandler
{
    public override void SetMauiContext(IMauiContext mauiContext)
    {
        var fooMauiContext = (FooMauiContext) mauiContext;
        WpfApplication = fooMauiContext.WpfApplication;
        base.SetMauiContext(mauiContext);
    }

    private System.Windows.Application? WpfApplication { get; set; }

    protected override void ConnectHandler(object platformView)
    {
        base.ConnectHandler(platformView);
    }

    protected override void DisconnectHandler(object platformView)
    {
        base.DisconnectHandler(platformView);
    }

    public override void SetVirtualView(IElement view)
    {
        base.SetVirtualView(view);
    }

    protected override object CreatePlatformElement()
    {
        Debug.Assert(WpfApplication != null);
        return WpfApplication;
    }
}