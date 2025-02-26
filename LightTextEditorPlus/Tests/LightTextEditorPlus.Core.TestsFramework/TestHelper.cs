using LightTextEditorPlus.Core.Document;

namespace LightTextEditorPlus.Core.TestsFramework;

public static class TestHelper
{
    public const string PlainNumberText = "123";
    public const string PlainLongNumberText = "1231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231231"; // 200 个字符

    /// <summary>
    /// 默认的固定字符布局的字号
    /// </summary>
    public const double DefaultFixCharFontSize = 15;

    /// <summary>
    /// 默认固定字符的默认字号的字宽
    /// </summary>
    public const double DefaultFixCharWidth = DefaultFixCharFontSize;

    /// <summary>
    /// 不能放在行首的字符
    /// </summary>
    /// 从 Word 抄
    public static char[] PunctuationNotInLineStartCharList => new[]
    {
        ',', '.', ';', '!', '，', '。', '！', '：', '；', '、', ')', '）'
    };

    public static TextEditorCore GetTextEditorCore(TestPlatformProvider? testPlatformProvider = null)
    {
        testPlatformProvider ??= new TestPlatformProvider();
        var textEditorCore = new TextEditorCore(testPlatformProvider);

        if (testPlatformProvider.CharInfoMeasurer is FixedCharSizeCharInfoMeasurer)
        {
            // 如果是固定字符尺寸测量的，那就设置默认字体就是 15 号
            textEditorCore.DocumentManager.SetStyleTextRunProperty<LayoutOnlyRunProperty>(runProperty => runProperty with
            {
                FontSize = DefaultFixCharFontSize
            });
        }

        return textEditorCore;
    }

    /// <summary>
    /// 使用非常明确的布局。将内置使用 <see cref="TestPlatformProviderExtension.UsingFixedCharSizeCharInfoMeasurer"/> 方式
    /// </summary>
    /// <param name="testPlatformProvider"></param>
    /// <param name="lineCharCount">一行能够布局多少个字符</param>
    /// <param name="fontSize"></param>
    public static TextEditorCore GetLayoutTestTextEditor(int lineCharCount = 5, double fontSize = LayoutTestFontSize,
        TestPlatformProvider? testPlatformProvider = null)
    {
        testPlatformProvider ??= new TestPlatformProvider();
        // 使用固定字符尺寸计算，返回字符尺寸等于字号，方便计算
        testPlatformProvider.UsingFixedCharSizeCharInfoMeasurer();
        testPlatformProvider.UseFakeLineSpacingCalculator();

        // 设置 20 字号，方便行距计算
        testPlatformProvider.FakePlatformRunPropertyCreator = new FakePlatformRunPropertyCreator
        (
            getDefaultRunPropertyFunc: () => new LayoutOnlyRunProperty()
            {
                FontSize = fontSize
            }
        );

        var textEditor = new TextEditorCore(testPlatformProvider);
        // 设置一行能布局 lineCharCount 个字
        textEditor.DocumentManager.DocumentWidth = fontSize * lineCharCount + 0.1;
        return textEditor;
    }

    public const double LayoutTestFontSize = 20;
    public const double LayoutTestCharWidth = LayoutTestFontSize;
    public const double LayoutTestCharHeight = LayoutTestFontSize;

    public static IReadOnlyRunProperty CreateRunProperty(this TextEditorCore textEditor, ConfigReadOnlyRunProperty<LayoutOnlyRunProperty> config)
    {
        LayoutOnlyRunProperty runProperty = (LayoutOnlyRunProperty) textEditor.DocumentManager.StyleRunProperty;
        return config(runProperty);
    }
}
