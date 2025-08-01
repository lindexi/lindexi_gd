using LightTextEditorPlus.Core.Document.Segments;

namespace LightTextEditorPlus.Core.Layout.HitTests;

readonly record struct ParagraphHitTestResult(bool Success,
    TextHitTestResult Result,
    DocumentOffset CurrentDocumentOffset)
{
    public static ParagraphHitTestResult OnSuccess(TextHitTestResult result)
        => default(ParagraphHitTestResult) with
        {
            Success = true,
            Result = result
        };

    public static ParagraphHitTestResult OnFail(DocumentOffset currentDocumentOffset)
        => default(ParagraphHitTestResult) with
        {
            Success = false,
            CurrentDocumentOffset = currentDocumentOffset
        };
}