using System;
using System.Windows.Media.Media3D;

using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Platform;
using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Document;

class RunPropertyCreator : PlatformRunPropertyCreatorBase<RunProperty>
{
    public RunPropertyCreator(TextEditor textEditor)
    {
        TextEditor = textEditor;
        _runPropertyPlatformManager = new RunPropertyPlatformManager(textEditor);
    }

    public TextEditor TextEditor { get; }

    protected override RunProperty OnBuildNewProperty(Action<IReadOnlyRunProperty> config, RunProperty baseRunProperty)
    {
        var runProperty = new RunProperty(_runPropertyPlatformManager, baseRunProperty);
        config(runProperty);
        return runProperty;
    }

    protected override RunProperty OnGetDefaultRunProperty()
    {
        return new RunProperty(_runPropertyPlatformManager);
    }

    private readonly RunPropertyPlatformManager _runPropertyPlatformManager;
}