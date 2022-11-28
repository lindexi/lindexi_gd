using System;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Platform;

namespace LightTextEditorPlus.Document;

class RunPropertyCreator : PlatformRunPropertyCreatorBase<RunProperty>
{
    protected override RunProperty OnBuildNewProperty(Action<IReadOnlyRunProperty> config, RunProperty baseRunProperty)
    {
        var runProperty = new RunProperty(baseRunProperty);
        config(runProperty);
        return runProperty;
    }

    protected override RunProperty OnGetDefaultRunProperty()
    {
        return new RunProperty();
    }
}