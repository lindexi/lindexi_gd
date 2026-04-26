using System.Text;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Document;

namespace LightTextEditorPlus.Avalonia.Tests;

[TestClass]
public class SkiaTextEditorTest
{
    [TestMethod("测试传入 emoji 表情和空格内容，预期可以正常布局")]
    public void MeasureEmojiCharData()
    {
        var skiaTextEditor = new SkiaTextEditor();
        var styleRunProperty = (SkiaTextRunProperty) skiaTextEditor.TextEditorCore.DocumentManager.StyleRunProperty;
        Rune emojiRune = new Rune(0x1F9EA); // 🧪
        skiaTextEditor.AppendRun(new SkiaTextRun($"{emojiRune} ", styleRunProperty with
        {
            FontName = new FontName("Segoe UI Emoji")
        }));
        var currentTextRender = skiaTextEditor.GetCurrentTextRender();
        currentTextRender.Dispose();
    }
}