namespace LightTextEditorPlus.Core.Layout.HitTests;

readonly record struct LineHitTestResult(ParagraphHitTestResult ParagraphHitTestResult)
{
    public static implicit operator LineHitTestResult(ParagraphHitTestResult result)
    {
        return new LineHitTestResult(result);
    }
}