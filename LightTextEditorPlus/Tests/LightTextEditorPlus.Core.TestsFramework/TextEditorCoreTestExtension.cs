namespace LightTextEditorPlus.Core.TestsFramework;

public static class TextEditorCoreTestExtension
{
    public static TextEditorCore UseFixedLineSpacing(this TextEditorCore textEditorCore, double fixedLineSpacing = 15)
    {
        textEditorCore.DocumentManager.CurrentParagraphProperty =
            textEditorCore.DocumentManager.CurrentParagraphProperty with
            {
                FixedLineSpacing = fixedLineSpacing
            };

        return textEditorCore;
    }
}