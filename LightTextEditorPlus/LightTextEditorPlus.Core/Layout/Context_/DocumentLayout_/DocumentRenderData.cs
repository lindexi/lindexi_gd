using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Core.Layout;

///// <summary>
///// 文档渲染信息
///// </summary>
//class DocumentRenderData
//{
//    /// <summary>
//    /// 文档的边界
//    /// </summary>
//    /// <remarks>
//    /// 可能小于 DocumentWidth 和 DocumentHeight 的值
//    /// </remarks>
//    public TextRect DocumentBounds { set; get; }
//}

/// <summary>
/// 文档布局范围
/// </summary>
/// <param name="DocumentContentBounds">内容范围，可能小于 DocumentWidth 和 DocumentHeight 的值</param>
/// <param name="DocumentOutlineBounds">外接范围。外接范围的左上角是 0,0 点</param>
public readonly record struct DocumentLayoutBounds(TextRect DocumentContentBounds, TextRect DocumentOutlineBounds)
{
}