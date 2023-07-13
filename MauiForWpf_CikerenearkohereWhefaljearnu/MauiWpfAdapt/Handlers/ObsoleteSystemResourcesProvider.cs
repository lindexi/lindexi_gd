using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.Controls.Internals;
using ResourceDictionary = Microsoft.Maui.Controls.ResourceDictionary;
using Microsoft.Maui.Controls;
using Button = Microsoft.Maui.Controls.Button;

namespace MauiWpfAdapt.Handlers;

class ObsoleteSystemResourcesProvider : ISystemResourcesProvider
{
    public IResourceDictionary GetSystemResources()
    {
        return new ResourceDictionary();
    }
}