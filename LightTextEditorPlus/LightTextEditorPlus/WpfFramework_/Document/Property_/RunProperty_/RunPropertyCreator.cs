using System;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Platform;

namespace LightTextEditorPlus.Document;

class RunPropertyCreator : PlatformRunPropertyCreatorBase<RunProperty>
{
    public RunPropertyCreator()
    {
    }

    protected override RunProperty OnBuildNewProperty(Action<IReadOnlyRunProperty> config, RunProperty baseRunProperty)
    {
        var runProperty = new RunProperty(_runPropertyPlatformManager,baseRunProperty);
        config(runProperty);
        return runProperty;
    }

    protected override RunProperty OnGetDefaultRunProperty()
    {
        return new RunProperty(_runPropertyPlatformManager);
    }

    private readonly RunPropertyPlatformManager _runPropertyPlatformManager = new RunPropertyPlatformManager();
}

/// <summary>
/// 用来管理本地资源，包括画刷或者是字体等
/// </summary>
class RunPropertyPlatformManager
{

}