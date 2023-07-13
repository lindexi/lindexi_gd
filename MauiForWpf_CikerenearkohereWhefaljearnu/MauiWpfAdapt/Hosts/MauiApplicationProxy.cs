using Microsoft.Maui;

namespace MauiWpfAdapt.Hosts;

public class MauiApplicationProxy
{
    public IApplication Application { get; }

    internal MauiApplicationProxy(IApplication application)
    {
        Application = application;
    }
}