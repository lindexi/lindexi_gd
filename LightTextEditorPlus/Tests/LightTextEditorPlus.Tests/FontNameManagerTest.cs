using LightTextEditorPlus.Core.Platform;
using dotnetCampus.UITest.WPF;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Utils;
using LightTextEditorPlus.Document;
using Moq;
using MSTest.Extensions.Contracts;

namespace LightTextEditorPlus.Tests;

[TestClass]
public class FontNameManagerTest
{
    [UIContractTestCase]
    public void RegisterFontFallback()
    {
        "注入自定义的字体回滚，如果传入不存在的字体，则会进入回滚逻辑".Test(() =>
        {
            using TextEditTestContext testContext = TestFramework.CreateTextEditorInNewWindow();
            var textEditor = testContext.TextEditor;

            const string fontName = "一个不存在的字体xxxxasdasd";
            var mock = new Mock<IFontNameManager>();

            mock.Setup(t => t.GetFallbackFontName(fontName, textEditor.TextEditorCore)).Returns("名为回滚的字体");

            textEditor.TextEditorCore.FontNameManager = mock.Object;

            RunProperty runProperty = (RunProperty) textEditor.CurrentCaretRunProperty with
            {
                FontName = new FontName(fontName)
            };
            runProperty.GetGlyphTypeface();

            mock.Verify(t => t.GetFallbackFontName(fontName, textEditor.TextEditorCore), Times.Once);
        });

        "对文本进行注册字体回滚，可以成功注册".Test(() =>
        {
            TextContext.FontNameManager.UseDefaultFontFallbackRules();
            // 没有抛异常就是成功
        });

        "如果字体找不到回滚，将会触发字体回滚失败事件".Test(() =>
        {
            const string fontName = "一个不存在的字体xxxxasdasd";
            var count = 0;

            TextContext.FontNameManager.FontFallbackFailed += (sender, args) =>
            {
                if (args.FontName.Equals(fontName, StringComparison.Ordinal))
                {
                    count++;
                }
            };

            using TextEditTestContext testContext = TestFramework.CreateTextEditorInNewWindow();
            var (mainWindow, textEditor) = testContext;
            var runPropertyCreator = textEditor.TextEditorPlatformProvider.GetPlatformRunPropertyCreator();
            var styleRunProperty = runPropertyCreator.GetDefaultRunProperty().AsRunProperty() with
            {
                FontName = new FontName(fontName)
            };

            styleRunProperty.GetGlyphTypeface();

            Assert.AreEqual(true, count > 0);
        });
    }
}
