using System.Windows.Controls;
using MauiWpfAdapt.Hosts;
using Microsoft.Maui;

namespace MauiWpfAdapt.Handlers;

class FooTreeMauiContext : FooAdaptMauiContext
{
    public FooTreeMauiContext(Panel panel, IMauiContext context) : base(context)
    {
        Panel = panel;
    }

    public Panel Panel { get; }
}