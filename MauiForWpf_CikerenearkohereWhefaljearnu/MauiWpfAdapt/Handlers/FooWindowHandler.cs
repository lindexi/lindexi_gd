using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;

namespace MauiWpfAdapt.Handlers;

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