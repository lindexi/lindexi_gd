using Microsoft.Maui.Controls;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using Microsoft.Maui;

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace WhineekelorerjarloFawyerwallqee;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public App()
    {
        MauiProgram
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
}
