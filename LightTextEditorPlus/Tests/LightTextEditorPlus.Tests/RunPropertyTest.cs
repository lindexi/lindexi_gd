using dotnetCampus.UITest.WPF;
using LightTextEditorPlus.Core;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Document;

using MSTest.Extensions.Contracts;
using RunProperty = LightTextEditorPlus.Document.RunProperty;

namespace LightTextEditorPlus.Tests;

[TestClass]
public class RunPropertyTest
{
    [UIContractTestCase]
    public void GetGlyphTypeface()
    {
        "没有修改 RunProperty 的字体相关属性，可以获取和样式相同的 GlyphTypeface 对象".Test(() =>
        {
            var (mainWindow, textEditor) = TestFramework.CreateTextEditorInNewWindow();
            var runPropertyCreator = textEditor.TextEditorPlatformProvider.GetPlatformRunPropertyCreator();

            var styleRunProperty = runPropertyCreator.GetDefaultRunProperty().AsRunProperty() with
            {
                FontName = new FontName("Arial")
            };

            var glyphTypeface1 = styleRunProperty.GetGlyphTypeface();

            var runProperty = styleRunProperty with
            {
                // 没有修改 RunProperty 的字体相关属性
            };

            var glyphTypeface2 = runProperty.GetGlyphTypeface();

            Assert.AreSame(glyphTypeface1, glyphTypeface2);
        });

        "修改 RunProperty 对象字体名，可以获取到字体的 GlyphTypeface 对象".Test(() =>
        {
            var (mainWindow, textEditor) = TestFramework.CreateTextEditorInNewWindow();
            var runPropertyCreator = textEditor.TextEditorPlatformProvider.GetPlatformRunPropertyCreator();

            var runProperty = runPropertyCreator.GetDefaultRunProperty().AsRunProperty() with
            {
                FontName = new FontName("Arial")
            };

            var glyphTypeface = runProperty.GetGlyphTypeface();

            // 这个字体，理论上是不会存在别名的
            Assert.AreEqual("Arial", glyphTypeface.FamilyNames.Values.First());
        });

        "给定 RunProperty 对象，可以获取到字体的 GlyphTypeface 对象".Test(() =>
        {
            var (mainWindow, textEditor) = TestFramework.CreateTextEditorInNewWindow();
            var runPropertyCreator = textEditor.TextEditorPlatformProvider.GetPlatformRunPropertyCreator();

            var runProperty = runPropertyCreator.GetDefaultRunProperty().AsRunProperty();

            var glyphTypeface = runProperty.GetGlyphTypeface();
            Assert.IsNotNull(glyphTypeface);
        });
    }

    [UIContractTestCase]
    public void Equals()
    {
        "获取的两个默认的 RunProperty 对象，判断相等，返回相等".Test(() =>
        {
            var (mainWindow, textEditor) = TestFramework.CreateTextEditorInNewWindow();
            var runPropertyCreator = textEditor.TextEditorPlatformProvider.GetPlatformRunPropertyCreator();

            // 获取的两个默认的 RunProperty 对象
            var runProperty1 = runPropertyCreator.GetDefaultRunProperty();
            var runProperty2 = runPropertyCreator.GetDefaultRunProperty();

            // 判断相等，返回相等
            var equals = runProperty1.Equals(runProperty2);
            Assert.AreEqual(true, equals);
        });
    }
}
