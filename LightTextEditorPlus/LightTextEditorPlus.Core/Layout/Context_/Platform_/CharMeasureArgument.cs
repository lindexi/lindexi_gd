//using LightTextEditorPlus.Core.Document;
//using LightTextEditorPlus.Core.Primitive.Collections;

//namespace LightTextEditorPlus.Core.Layout;

///// <summary>
///// 测量字符信息参数
///// </summary>
///// <param name="CurrentCharData"></param>
///// <param name="RunList"></param>
///// <param name="CurrentIndex"></param>
///// <param name="Paragraph"></param>
///// <param name="UpdateLayoutContext"></param>
//public readonly record struct CharMeasureArgument
//(
//    CharData CurrentCharData,
//    TextReadOnlyListSpan<CharData> RunList,
//    int CurrentIndex,
//    ITextParagraph Paragraph,
//    UpdateLayoutContext UpdateLayoutContext
//)
//{
//    public CharInfo CurrentCharInfo => CurrentCharData.ToCharInfo();
//}