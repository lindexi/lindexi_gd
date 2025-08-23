using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Document.Segments;
using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Core.Layout.HitTests;

/// <summary>
/// 段落命中测试的上下文
/// </summary>
/// <param name="HitPoint"></param>
/// <param name="ParagraphData"></param>
/// <param name="ParagraphIndex"></param>
/// <param name="StartDocumentOffset"></param>
/// <param name="LogContext"></param>
readonly record struct ParagraphHitTestContext
(
    TextPoint HitPoint,
    ParagraphData ParagraphData,
    ParagraphIndex ParagraphIndex,
    DocumentOffset StartDocumentOffset,
    TextEditorDebugLogContext LogContext
);