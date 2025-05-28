using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Primitive.Collections;
using LightTextEditorPlus.Core.Rendering;

namespace LightTextEditorPlus.Document.Decorations;

/// <summary>
/// 构建装饰的参数
/// </summary>
/// <param name="RunProperty"></param>
/// <param name="CharDataList">当前相同属性的字符列表，从准备绘制的起始字符开始</param>
/// <param name="RecommendedBounds">推荐的渲染范围</param>
/// <param name="LineRenderInfo">段落内的行的渲染信息</param>
///// <param name="CurrentCharIndexInLine">当前准备绘制的起始字符所在当前行的坐标</param>
public readonly record struct BuildDecorationArgument(IRunProperty RunProperty, /*int CurrentCharIndexInLine,*/ TextReadOnlyListSpan<CharData> CharDataList, TextRect RecommendedBounds, ParagraphLineRenderInfo LineRenderInfo, TextEditor TextEditor);