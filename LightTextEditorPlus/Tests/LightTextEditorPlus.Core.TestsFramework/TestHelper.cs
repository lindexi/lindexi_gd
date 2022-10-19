namespace LightTextEditorPlus.Core.Tests;

public static class TestHelper
{
    public const string PlainNumberText = "123";
    public const string PlainLongNumberText = "123123123123123123123123123123123123123123123123123123123123123123123123123123123123123123123123123123123123123123123123123123123123123123123123123123123123123123123123123123123123";

    public static TextEditorCore GetTextEditorCore()
    {
        var testPlatformProvider = new TestPlatformProvider();
        var textEditorCore = new TextEditorCore(testPlatformProvider);
        return textEditorCore;
    }
}