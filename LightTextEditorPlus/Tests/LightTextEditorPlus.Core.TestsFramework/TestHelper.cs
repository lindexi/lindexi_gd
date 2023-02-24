namespace LightTextEditorPlus.Core.TestsFramework;

public static class TestHelper
{
    public const string PlainNumberText = "123";
    public const string PlainLongNumberText = "1231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231"; // 200 ¸ö×Ö·û

    public static TextEditorCore GetTextEditorCore(TestPlatformProvider? testPlatformProvider = null)
    {
        testPlatformProvider ??= new TestPlatformProvider();
        var textEditorCore = new TextEditorCore(testPlatformProvider);
        return textEditorCore;
    }
}