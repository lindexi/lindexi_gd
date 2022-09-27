namespace LightTextEditorPlus.Core.Tests;

static class TestHelper
{
    public const string PlainNumberText = "123";

    public static TextEditorCore GetTextEditorCore()
    {
        var testPlatformProvider = new TestPlatformProvider();
        var textEditorCore = new TextEditorCore(testPlatformProvider);
        return textEditorCore;
    }
}