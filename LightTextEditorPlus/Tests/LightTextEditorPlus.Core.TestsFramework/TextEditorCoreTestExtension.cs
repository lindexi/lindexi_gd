namespace LightTextEditorPlus.Core.TestsFramework;

public static class TextEditorCoreTestExtension
{
    public static TextEditorCore UseFixedLineSpacing(this TextEditorCore textEditorCore, double fixedLineSpacing = 15)
    {
        textEditorCore.DocumentManager.DefaultParagraphProperty =
            textEditorCore.DocumentManager.DefaultParagraphProperty with
            {
                FixedLineSpacing = fixedLineSpacing
            };

        return textEditorCore;
    }
}