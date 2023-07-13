using System.Windows;
using System.Windows.Controls;
using MauiWpfAdapt.Hosts;
using Microsoft.Maui;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using Rect = Microsoft.Maui.Graphics.Rect;
using Size = Microsoft.Maui.Graphics.Size;

namespace MauiWpfAdapt.Handlers;

class FooPageHandler : PageHandler
{
    public FooPageHandler() : base(new PropertyMapper<IContentView, IPageHandler>(PageHandler.Mapper)
    {
        [nameof(IContentView.Content)] = MapFooContent
    })
    {
    }

    public override void SetMauiContext(IMauiContext mauiContext)
    {
        var fooPanelMauiContext = (FooPanelMauiContext) mauiContext;
        _fooPanelMauiContext = fooPanelMauiContext;
        base.SetMauiContext(mauiContext);
    }

    public override void PlatformArrange(Rect rect)
    {
        base.PlatformArrange(rect);
        var size = VirtualView.CrossPlatformArrange(rect);
        var platformView = (Canvas) PlatformView;
        platformView.SetValue(Canvas.LeftProperty, rect.Left);
        platformView.SetValue(Canvas.TopProperty, rect.Top);
        platformView.Width = rect.Width;
        platformView.Height = rect.Height;
    }

    public override Size GetDesiredSize(double widthConstraint, double heightConstraint)
    {
        return VirtualView.CrossPlatformMeasure(widthConstraint, heightConstraint);
    }

    private FooPanelMauiContext? _fooPanelMauiContext;

    protected override object CreatePlatformView()
    {
        return _fooPanelMauiContext.Panel;
    }

    static void MapFooContent(IContentViewHandler handler, IContentView page)
    {
        var panel = (Panel) handler.PlatformView;
        var platform = (UIElement?) page.PresentedContent?.ToPlatform(new FooTreeMauiContext(panel, handler.MauiContext!));
        panel.Children.Clear();
        if (platform != null)
        {
            panel.Children.Add(platform);
        }
    }
}