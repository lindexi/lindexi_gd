using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Layout.LayoutUtils;

namespace LightTextEditorPlus.Core.Layout;

/// <summary>
/// 此文件的存在只是用来做引用记录，记录一些机制的实现
/// </summary>
/// 布局系统是最为复杂的部分
///
/// Q: 缩进是在哪处理的？
/// A: 缩进分为上下左右方向，其中上下方向成为竖直方向，为 段前间距 和 段后间距。对应在 <see cref="ParagraphProperty.ParagraphBefore"/> 和 <see cref="ParagraphProperty.ParagraphAfter"/> 属性。直接就在 <see cref="ParagraphLayoutArgumentLayoutUtilsExtension.GetParagraphBefore"/> 和 <see cref="ParagraphLayoutArgumentLayoutUtilsExtension.GetParagraphAfter"/> 方法获取，在 <see cref="HorizontalArrangingLayoutProvider.UpdateParagraphLayoutData"/> 方法直接设置到 <see cref="ParagraphData.SetParagraphLayoutContentThickness"/> 里面
/// 
/// 左右方向比较复杂，需要考虑项目符号的存在
/// 现在放在 <see cref="ArrangingLayoutProvider.CalculateParagraphIndent"/> 里面进行计算
file class ReadMe
{
}
