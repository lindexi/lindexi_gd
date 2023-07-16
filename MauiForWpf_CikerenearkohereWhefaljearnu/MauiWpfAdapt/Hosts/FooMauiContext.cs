using System;
using Microsoft.Maui;

namespace MauiWpfAdapt.Hosts;

class FooMauiContext : MauiContext
{
    public FooMauiContext(System.Windows.Application wpfApplication,IServiceProvider services) : base(services)
    {
        WpfApplication = wpfApplication;
    }

    public System.Windows.Application WpfApplication { get; }
}