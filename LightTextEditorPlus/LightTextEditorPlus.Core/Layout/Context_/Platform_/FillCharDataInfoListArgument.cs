using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Layout.LayoutUtils;
using LightTextEditorPlus.Core.Primitive.Collections;

namespace LightTextEditorPlus.Core.Layout;

/// <summary>
/// 填充字符列表的布局信息参数
/// </summary>
/// <param name="ToFillCharDataList">准备填充的字符列表</param>
/// <param name="UpdateLayoutContext"></param>
public readonly record struct FillCharDataInfoListArgument
(
    TextReadOnlyListSpan<CharData> ToFillCharDataList,
    UpdateLayoutContext UpdateLayoutContext
)
{
    /// <summary>
    /// 设置字符布局信息辅助工具
    /// </summary>
    public ICharDataLayoutInfoSetter CharDataLayoutInfoSetter => UpdateLayoutContext;
}