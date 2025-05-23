using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media.Media3D;

using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Exceptions;
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

    protected override RunProperty OnGetDefaultRunProperty()
    {
        return new RunProperty(_runPropertyPlatformManager);
    }

    private readonly RunPropertyPlatformManager _runPropertyPlatformManager;

    public override IReadOnlyRunProperty ToPlatformRunProperty(ICharObject charObject, IReadOnlyRunProperty baseRunProperty)
    {
        if (baseRunProperty is RunProperty runProperty)
        {
            if (!ReferenceEquals(runProperty.RunPropertyPlatformManager, _runPropertyPlatformManager))
            {
                // 是从其他平台创建的？
                var message = $"""
                               当前传入字符属性不是由当前文本的资源管理器所创建，可能将其他文本的字符属性传入给到当前文本。
                               请确保传入的字符属性由当前文本的资源管理器创建。
                               可使用 with 关键字从 DefaultRunProperty 属性创建出新的字符属性。
                               当前文本框DebugName='{TextEditor.TextEditorCore.DebugName}';字符属性RunProperty所在资源的文本框DebugName='{runProperty.RunPropertyPlatformManager.TextEditor.TextEditorCore.DebugName}'
                               """;
                TextEditor. Logger.LogWarning(message);
                if (TextEditor.IsInDebugMode)
                {
                    throw new TextEditorDebugException(message);
                }

                // 尝试兼容
                runProperty = runProperty with
                {
                    RunPropertyPlatformManager = _runPropertyPlatformManager
                };
            }

            // todo 后续考虑在这里处理字符兼容性问题

            return runProperty;
        }
        else
        {
            // 让底层去抛出异常
            return base.ToPlatformRunProperty(charObject, baseRunProperty);
        }
    }

    protected override RunProperty OnUpdateMarkerRunProperty(RunProperty? markerRunProperty, RunProperty styleRunProperty)
    {
        if (markerRunProperty is null)
        {
            return styleRunProperty;
        }
        else
        {
            return styleRunProperty with
            {
                FontName = markerRunProperty.FontName,
            };
        }
    }
}