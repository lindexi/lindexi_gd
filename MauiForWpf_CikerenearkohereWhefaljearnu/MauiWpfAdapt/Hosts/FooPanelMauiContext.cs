using System.Windows.Controls;
using Microsoft.Maui;

namespace MauiWpfAdapt.Hosts;

class FooPanelMauiContext : FooAdaptMauiContext
{
    public FooPanelMauiContext(Panel panel,IMauiContext context) : base(context)
    {
        Panel = panel;
    }

    public Panel Panel { get; }
}