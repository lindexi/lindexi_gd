using System.Reflection;

using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.TestsFramework;

using MSTest.Extensions.Contracts;

namespace LightTextEditorPlus.Core.Tests;

[TestClass]
public class TextEditorUndoRedoTest
{
    [ContractTestCase]
    public void SetUndoRedoEnable()
    {
        "设置文本禁用撤销重做，后又开启撤销重做，开启后文本追加可以加入撤销恢复".Test(() =>
        {
            // Arrange
            var testTextEditorUndoRedoProvider = new TestTextEditorUndoRedoProvider();
            var textEditorCore = TestHelper.GetTextEditorCore(new TestPlatformProvider()
            {
                UndoRedoProvider = testTextEditorUndoRedoProvider
            });

            // Action
            // 设置文本禁用撤销重做
            textEditorCore.SetUndoRedoEnable(false, "Test");
            // 文本追加等将不会加入撤销恢复
            textEditorCore.AppendText(TestHelper.PlainNumberText);

            // 重新开启撤销重做
            textEditorCore.SetUndoRedoEnable(true, "Test");
            // 再次追加内容，预期追加的内容加入到撤销重做
            textEditorCore.AppendText(TestHelper.PlainNumberText);

            // Assert
            // 只加入一条到撤销恢复
            Assert.AreEqual(1, testTextEditorUndoRedoProvider.UndoOperationList.Count);
        });

        "设置文本禁用撤销重做，文本追加等将不会加入撤销恢复".Test(() =>
        {
            // Arrange
            var testTextEditorUndoRedoProvider = new TestTextEditorUndoRedoProvider();
            var textEditorCore = TestHelper.GetTextEditorCore(new TestPlatformProvider()
            {
                UndoRedoProvider = testTextEditorUndoRedoProvider
            });

            // Action
            // 设置文本禁用撤销重做
            textEditorCore.SetUndoRedoEnable(false, "Test");
            // 文本追加等将不会加入撤销恢复
            textEditorCore.AppendText(TestHelper.PlainNumberText);

            // Assert
            // 不会加入撤销恢复，也就是撤销恢复空列表
            Assert.AreEqual(0, testTextEditorUndoRedoProvider.UndoOperationList.Count);
        });
    }

    [ContractTestCase]
    public void UndoRedo()
    {
        "替换文本字符串里中间的字符，可以通过撤销撤回更改，再次调用恢复可以回到原本样式的文本".Test(() =>
        {
            // Arrange
            var testTextEditorUndoRedoProvider = new TestTextEditorUndoRedoProvider();
            var textEditorCore = TestHelper.GetTextEditorCore(new TestPlatformProvider()
            {
                UndoRedoProvider = testTextEditorUndoRedoProvider
            });
            // 禁用撤销重做，减少干扰
            textEditorCore.SetUndoRedoEnable(false, "test");
            // 先追加点文本，用来后续进行替换
            textEditorCore.AppendText("abc");
            // 重新开撤销重做
            textEditorCore.SetUndoRedoEnable(true, "test");
            // 替换文本字符串里中间的字符
            var selection = new Selection(new CaretOffset(1), 1);
            const string fontName = "测试用的字体";
            var runProperty = new LayoutOnlyRunProperty()
            {
                FontName = new FontName(fontName)
            };

            // Action
            textEditorCore.EditAndReplaceRun(new TextRun("d", runProperty), selection);

            // Assert
            // 可以通过撤销撤回更改
            Assert.AreEqual(1, testTextEditorUndoRedoProvider.UndoOperationList.Count);

            // 撤销一下，返回原先的字符串
            testTextEditorUndoRedoProvider.Undo();
            Assert.AreEqual("abc", textEditorCore.GetText());

            // 恢复一下
            testTextEditorUndoRedoProvider.Redo();
            Assert.AreEqual("adc", textEditorCore.GetText());

            // 样式也能恢复
            var runPropertyList = textEditorCore.DocumentManager.GetDifferentRunPropertyRange(textEditorCore.GetAllDocumentSelection()).ToList();
            // 只有中间一个不同，因此获取不相同的应该是3个，证明前后两个字符和中间的字符的字符属性不同
            Assert.AreEqual(3, runPropertyList.Count);
            Assert.AreEqual(fontName, runPropertyList[1].FontName.UserFontName);
        });

        "追加带样式的文本之后，可以通过撤销撤回更改，再次调用恢复可以回到原本样式的文本".Test(() =>
        {
            // Arrange
            var testTextEditorUndoRedoProvider = new TestTextEditorUndoRedoProvider();
            var textEditorCore = TestHelper.GetTextEditorCore(new TestPlatformProvider()
            {
                UndoRedoProvider = testTextEditorUndoRedoProvider
            });

            // Action
            // 追加带样式的文本
            const string fontName = "测试用的字体";
            var platformRunPropertyCreator = textEditorCore.PlatformProvider.GetPlatformRunPropertyCreator();
            var runProperty = platformRunPropertyCreator.BuildNewProperty(t => ((LayoutOnlyRunProperty) t).FontName = new FontName(fontName),
                platformRunPropertyCreator.GetDefaultRunProperty());

            textEditorCore.AppendRun(new TextRun(TestHelper.PlainNumberText, runProperty));

            // Assert
            // 可以通过撤销撤回更改
            Assert.AreEqual(1, testTextEditorUndoRedoProvider.UndoOperationList.Count);

            // 撤销一下
            testTextEditorUndoRedoProvider.Undo();

            // 撤销完成，那就是空文本了
            Assert.AreEqual(0, textEditorCore.DocumentManager.CharCount);

            // Action
            // 通过恢复加上原本样式的文本
            testTextEditorUndoRedoProvider.Redo();

            // Assert
            var runPropertyList = textEditorCore.DocumentManager.GetRunPropertyRange(textEditorCore.GetAllDocumentSelection()).ToList();
            Assert.AreEqual(3, runPropertyList.Count);
            Assert.AreEqual(fontName, runPropertyList[0].FontName.UserFontName);

            // 文档的三个字符的字符属性都是相同的，因此获取不同的字符属性只获取到一个
            Assert.AreEqual(1, textEditorCore.DocumentManager.GetDifferentRunPropertyRange(textEditorCore.GetAllDocumentSelection()).Count());
        });

        "追加文本之后，可以通过撤销重做撤回更改".Test(() =>
        {
            // Arrange
            var testTextEditorUndoRedoProvider = new TestTextEditorUndoRedoProvider();
            var textEditorCore = TestHelper.GetTextEditorCore(new TestPlatformProvider()
            {
                UndoRedoProvider = testTextEditorUndoRedoProvider
            });

            // Action
            textEditorCore.AppendText(TestHelper.PlainNumberText);

            // Assert
            // 会自动加入撤销重做
            Assert.AreEqual(1, testTextEditorUndoRedoProvider.UndoOperationList.Count);

            // 撤销一下
            testTextEditorUndoRedoProvider.Undo();

            // 撤销完成，那就是空文本了
            Assert.AreEqual(0, textEditorCore.DocumentManager.CharCount);

            // 继续重做，那就是存在文本
            testTextEditorUndoRedoProvider.Redo();
            Assert.AreEqual(TestHelper.PlainNumberText, textEditorCore.GetText());
        });
    }
}