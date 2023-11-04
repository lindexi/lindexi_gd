using System;
using Microsoft.Maui;

namespace MauiWpfAdapt.Hosts;

abstract class FooAdaptMauiContext : IMauiContext
{
    protected FooAdaptMauiContext(IMauiContext context)
    {
        Context = context;
    }

    public IMauiContext Context { get; }
    public IServiceProvider Services => Context.Services;

    public IMauiHandlersFactory Handlers => Context.Handlers;
}