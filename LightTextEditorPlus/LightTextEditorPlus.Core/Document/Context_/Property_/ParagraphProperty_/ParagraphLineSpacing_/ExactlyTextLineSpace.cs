namespace LightTextEditorPlus.Core.Document;

/// <summary>
/// 固定行距
/// </summary>
/// <param name="ExactlyLineHeight">单位与字号相同，为具体框架的单位</param>
/// 原本是 Fixed LineSpacing 的叫法，此叫法来自于头像君。但后续根据我的考证，感觉应该叫 Exact LineSpacing 更为合适。其原因如下
/// 在 Word 里面，称为行距固定值，使用 LineSpacingRuleValues.Exact 表示
/// 根据文档 https://learn.microsoft.com/en-us/dotnet/api/documentformat.openxml.wordprocessing.linespacingrulevalues?view=openxml-3.0.1 所述
/// Exact： Exact Line Height.
/// 在 PPT 里面，放在 DocumentFormat.OpenXml.Drawing.LineSpacing 里面的 SpacingPoints 里
/// 根据文档 https://learn.microsoft.com/en-us/dotnet/api/documentformat.openxml.drawing.spacingpoints?view=openxml-3.0.1 和 ISO/IEC 29500 规范所述和 ECMA 376 21.1.2.2.12 规范所述
/// spcPts (Spacing Points)： This element specifies the amount of white space that is to be used between lines and paragraphs in the form of a text point size. The size is specified using points where 100 is equal to 1 point font and 1200 is equal to 12 point.
/// 在 Word 里面可能被称为 WdLineSpacing.wdLineSpaceExactly 类型： Line spacing is only the exact maximum amount of space required. This setting commonly uses less space than single spacing.
public sealed record ExactlyTextLineSpace(double ExactlyLineHeight) : ITextLineSpacing
{
}
