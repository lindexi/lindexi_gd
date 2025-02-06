using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Document.Segments;

namespace LightTextEditorPlus.Core.Layout.HitTests;

readonly record struct LineHitTestContext(LineLayoutData LineLayoutData, DocumentOffset StartDocumentOffset, ParagraphHitTestContext ParagraphHitTestContext);