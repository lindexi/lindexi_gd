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
/// 现在放在 <see cref="ArrangingLayoutProvider.CalculateParagraphIndentAndMarker"/> 里面进行计算
///
/// Q: 项目符号是如何参与布局计算的？
/// A: 在段落布局之前，先调用 <see cref="ArrangingLayoutProvider.CalculateParagraphIndentAndMarker"/> 方法计算段落的缩进和项目符号的缩进贡献。此时项目符号的 CharData 将被测量，但还没被设置左上角坐标点。在 <see cref="HorizontalArrangingLayoutProvider.PreUpdateMarker"/> 里面设置项目符号 Y 坐标垂直方向的行距等信息
///
/// Q: 未变更的段落，是否在每次布局的时候，更新版本？
/// A: 取决于非脏的段落的状态。如果非脏段落前面有脏段，则更新版本。如果非脏段落前面无任何脏段，则布局过程将被跳过，不更新版本。非脏段落前面有脏段，在 <see cref="HorizontalArrangingLayoutProvider.UpdateNotDirtyParagraphStartPoint"/> 里面会确保没有脏的段落会更新版本
/// Q: 如果段落版本更新了，那段落里面的字符是否也需要跟着更新版本？
/// A: 是的。将在 <see cref="HorizontalArrangingLayoutProvider.UpdateNotDirtyParagraphStartPoint"/> 里面调用 <see cref="HorizontalArrangingLayoutProvider.UpdateParagraphLineLayoutDataStartPoint"/> 方法，更新段落里面每一行的版本。只要在脏段之后的任何段落，无论是否脏的段落，布局时都会更新版本。本身是脏的段落，原本就更新版本了
file class ReadMe
{
}
