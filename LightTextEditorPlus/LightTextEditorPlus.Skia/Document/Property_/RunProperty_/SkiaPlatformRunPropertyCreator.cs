using System;
using System.Diagnostics;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Exceptions;
using LightTextEditorPlus.Core.Platform;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Platform;
using SkiaSharp;

namespace LightTextEditorPlus.Document;

internal class SkiaPlatformRunPropertyCreator : PlatformRunPropertyCreatorBase<SkiaTextRunProperty>
{
    public SkiaPlatformRunPropertyCreator(SkiaPlatformResourceManager skiaPlatformResourceManager, SkiaTextEditor textEditor)
    {
        _skiaPlatformResourceManager = skiaPlatformResourceManager;
        _skiaTextEditor = textEditor;
    }

    private readonly SkiaPlatformResourceManager _skiaPlatformResourceManager;
    private ITextLogger Logger => _skiaTextEditor.Logger;
    private readonly SkiaTextEditor _skiaTextEditor;

    public override IReadOnlyRunProperty ToPlatformRunProperty(ICharObject charObject, IReadOnlyRunProperty baseRunProperty)
    {
        if (baseRunProperty is SkiaTextRunProperty skiaTextRunProperty)
        {
            if (!ReferenceEquals(skiaTextRunProperty.ResourceManager, _skiaPlatformResourceManager))
            {
                // 是从其他平台创建的？
                var message = $"""
                               当前传入字符属性不是由当前文本的资源管理器所创建，可能将其他文本的字符属性传入给到当前文本。
                               请确保传入的字符属性由当前文本的资源管理器创建。
                               可使用 with 关键字从 DefaultRunProperty 属性创建出新的字符属性。
                               当前文本框DebugName='{_skiaTextEditor.TextEditorCore.DebugName}';字符属性RunProperty所在资源的文本框DebugName='{skiaTextRunProperty.ResourceManager.SkiaTextEditor.TextEditorCore.DebugName}'
                               """;
                Logger.LogWarning(message);
                if (_skiaTextEditor.TextEditorCore.IsInDebugMode)
                {
                    throw new TextEditorDebugException(message);
                }

                // 尝试兼容
                skiaTextRunProperty = skiaTextRunProperty with
                {
                    ResourceManager = _skiaPlatformResourceManager
                };
            }

            return _skiaPlatformResourceManager.NormalRunProperty(skiaTextRunProperty, charObject);
        }
        else
        {
            // 让底层去抛出异常
            return base.ToPlatformRunProperty(charObject, baseRunProperty);
        }
    }

    public override IReadOnlyRunProperty UpdateMarkerRunProperty(IReadOnlyRunProperty? markerRunProperty,
        IReadOnlyRunProperty styleRunProperty)
    {
        if (styleRunProperty is not SkiaTextRunProperty styleSkiaTextRunProperty)
        {
            // 理论上不会进入此分支，因为这是从框架层传入的值
            ThrowRunPropertyTypeNotSupportedException(styleRunProperty);
            Debug.Fail("上面方法已经抛出异常了，理论上不会进入此分支");
            return null!;
        }

        if (markerRunProperty is null)
        {
            return styleRunProperty;
        }
        else
        {
            if (markerRunProperty is SkiaTextRunProperty markerSkiaTextRunProperty)
            {
                // 只保留字体名称，因为有些项目符号需要特殊字体，如 Wingdings 等
                return styleSkiaTextRunProperty with
                {
                    FontName = markerSkiaTextRunProperty.FontName,
                };
            }
            else
            {
                ThrowRunPropertyTypeNotSupportedException(markerRunProperty);
                Debug.Fail("上面方法已经抛出异常了，理论上不会进入此分支");
                return null!;
            }
        }
    }

    protected override SkiaTextRunProperty OnGetDefaultRunProperty()
    {
        // 默认字体
        var defaultFontName = SkiaPlatformResourceManager.GetDefaultFontName();

        return new SkiaTextRunProperty(_skiaPlatformResourceManager)
        {
            FontName = new FontName(defaultFontName),
        };
    }
}
