namespace LightTextEditorPlus.Core.Tests;

public static class TextEditorExtension
{
    public static string GetText(this TextEditorCore textEditor) =>
        textEditor.DocumentManager.TextRunManager.ParagraphManager.GetText();
}