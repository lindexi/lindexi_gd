//namespace LightTextEditorPlus.Core.Document;

///// <summary>
///// 表示一个 <see cref="IImmutableRun"/> 在 <see cref="ParagraphData"/> 的序号。此结构体不建议保存，因为段落状态将会变更
///// </summary>
///// <param name="ParagraphIndex">在段落里的索引序号</param>
///// <param name="Paragraph"></param>
///// <param name="HitRunIndex">命中到当前的 TextRun 的哪个字符</param>
///// <param name="ParagraphVersion">段落的更改版本</param>
//readonly record struct RunIndexInParagraph(int ParagraphIndex, ParagraphData Paragraph, IImmutableRun Run, int HitRunIndex,
//    uint ParagraphVersion)
//{
//    /// <summary>
//    /// 是否已无效
//    /// </summary>
//    public bool IsInvalid => Paragraph.IsInvalidVersion(ParagraphVersion);
//}