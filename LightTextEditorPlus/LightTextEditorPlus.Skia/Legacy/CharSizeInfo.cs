//using LightTextEditorPlus.Core.Document;
//using LightTextEditorPlus.Core.Primitive;

//namespace LightTextEditorPlus.Platform;

///// <summary>
///// 字符尺寸信息
///// </summary>
///// <param name="GlyphRunBounds">字符的外框，字外框</param>
//readonly record struct CharSizeInfo(TextRect GlyphRunBounds)
//{
//    /// <summary>
//    /// 字面尺寸，字墨尺寸，字墨大小。文字的字身框中，字图实际分布的空间的尺寸
//    /// </summary>
//    public TextSize TextFaceSize => CharDataInfo.FaceSize;

//    public required CharDataInfo CharDataInfo { get; init; }

//    /// <summary>
//    /// 文字外框，字外框尺寸
//    /// </summary>
//    public TextSize TextFrameSize => GlyphRunBounds.TextSize;
//}