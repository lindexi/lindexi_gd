using System;

using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Platform;

namespace LightTextEditorPlus.Core.TestsFramework;

public class FakePlatformRunPropertyCreator : IPlatformRunPropertyCreator
{
    public FakePlatformRunPropertyCreator(Func<IReadOnlyRunProperty>? getDefaultRunPropertyFunc = null, Func<ICharObject, IReadOnlyRunProperty, IReadOnlyRunProperty>? toPlatformRunPropertyFunc = null)
    {
        GetDefaultRunPropertyFunc = getDefaultRunPropertyFunc;
        ToPlatformRunPropertyFunc = toPlatformRunPropertyFunc;
    }

    public Func<IReadOnlyRunProperty>? GetDefaultRunPropertyFunc { get; set; }

    public Func<ICharObject, IReadOnlyRunProperty, IReadOnlyRunProperty>? ToPlatformRunPropertyFunc { get; set; }

    public IReadOnlyRunProperty GetDefaultRunProperty()
    {
        if (GetDefaultRunPropertyFunc != null)
        {
            return GetDefaultRunPropertyFunc();
        }

        return new LayoutOnlyRunProperty();
    }

    public IReadOnlyRunProperty ToPlatformRunProperty(ICharObject charObject, IReadOnlyRunProperty baseRunProperty)
    {
        if (ToPlatformRunPropertyFunc != null)
        {
            return ToPlatformRunPropertyFunc(charObject, baseRunProperty);
        }

        return baseRunProperty;
    }
}