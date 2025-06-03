#if DirectTextEditorDefinition

using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Primitive.Collections;
using LightTextEditorPlus.Core.Rendering;

#if USE_WPF
using Editor = LightTextEditorPlus.TextEditor;
#elif USE_SKIA
using SkiaSharp;
using RunProperty = LightTextEditorPlus.Document.SkiaTextRunProperty;
using Editor = LightTextEditorPlus.SkiaTextEditor;
#endif

namespace LightTextEditorPlus.Document.Decorations;

/// <summary>
/// 构建装饰的参数
/// </summary>
/// <param name="RunProperty"></param>
/// <param name="CurrentCharIndexInLine">当前准备绘制的起始字符所在当前行的坐标</param>
/// <param name="CharDataList">当前相同属性的字符列表，从准备绘制的起始字符开始</param>
/// <param name="RecommendedBounds">推荐的渲染范围</param>
/// <param name="LineRenderInfo">段落内的行的渲染信息</param>
/// <param name="TextEditor"></param>
public readonly record struct BuildDecorationArgument
(
    RunProperty RunProperty,
    int CurrentCharIndexInLine,
    TextReadOnlyListSpan<CharData> CharDataList,
    TextRect RecommendedBounds,
    ParagraphLineRenderInfo LineRenderInfo,
    Editor TextEditor
)
{
#if USE_SKIA
    /// <summary>
    /// 画布
    /// </summary>
    public required SKCanvas Canvas { get; init; }

    /// <summary>
    /// 缓存的画笔
    /// </summary>
    public required SKPaint CachePaint { get; init; }
#endif

    /// <inheritdoc cref="LightTextEditorPlus.Core.TextEditorCore.ArrangingType"/>
    public ArrangingType ArrangingType => TextEditor.TextEditorCore.ArrangingType;
}
#endif