using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Document.Segments;
using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Core.Layout.HitTests;

readonly record struct ParagraphHitTestContext(TextPoint HitPoint, ParagraphData ParagraphData, ParagraphIndex ParagraphIndex, DocumentOffset StartDocumentOffset);