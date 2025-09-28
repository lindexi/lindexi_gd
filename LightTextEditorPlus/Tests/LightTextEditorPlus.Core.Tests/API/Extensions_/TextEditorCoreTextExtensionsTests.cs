using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.TestsFramework;

using MSTest.Extensions.Contracts;

namespace LightTextEditorPlus.Core.Tests;

[TestClass()]
public class TextEditorCoreTextExtensionsTests
{
    [ContractTestCase]
    public void GetRunList()
    {
        "对包含换行的统一样式的文本调用 GetRunList 方法，可以返回一个包含一个 Run 内容的列表".Test(() =>
        {
            // Arrange
            var textEditorCore = TestHelper.GetTextEditorCore();
            textEditorCore.AppendText("123\r\nabc");

            // Action
            IImmutableRunList immutableRunList = textEditorCore.GetRunList(textEditorCore.GetAllDocumentSelection());

            // Assert
            // 可以返回一个包含空内容的列表
            Assert.AreEqual(1, immutableRunList.RunCount);
            Assert.AreEqual("123\nabc".Length, immutableRunList.CharCount);
        });

        "对包含两个不同的字符属性样式的文本调用 GetRunList 方法，可以返回一个包含两个 Run 内容的列表".Test(() =>
        {
            // Arrange
            var textEditorCore = TestHelper.GetTextEditorCore();
            LayoutOnlyRunProperty runProperty = (LayoutOnlyRunProperty) textEditorCore.DocumentManager.StyleRunProperty;

            // 包含两个不同的字符属性样式的文本
            textEditorCore.AppendRun(new TextRun("123", runProperty with
            {
                FontSize = 10
            }));
            textEditorCore.AppendRun(new TextRun("abc", runProperty with
            {
                FontSize = 100,
            }));

            // Action
            IImmutableRunList immutableRunList = textEditorCore.GetRunList(textEditorCore.GetAllDocumentSelection());

            // Assert
            // 可以返回一个包含空内容的列表
            Assert.AreEqual(2, immutableRunList.RunCount);
            Assert.AreEqual("123abc".Length, immutableRunList.CharCount);
        });

        "对有内容的调用 GetRunList 方法，传入空选择范围，可以返回一个不包含内容的列表".Test(() =>
        {
            // Arrange
            var textEditorCore = TestHelper.GetTextEditorCore();
            textEditorCore.AppendText("123");

            // Action
            // 传入空选择范围
            Selection selection = new Selection(new CaretOffset(0), 0);
            IImmutableRunList immutableRunList = textEditorCore.GetRunList(selection);

            // Assert
            // 可以返回一个包含空内容的列表
            Assert.AreEqual(0, immutableRunList.RunCount);
            Assert.AreEqual(0, immutableRunList.CharCount);
        });

        "对包含相同样式的文本调用 GetRunList 方法，可以返回一个包含一个 Run 内容的列表".Test(() =>
        {
            // Arrange
            var textEditorCore = TestHelper.GetTextEditorCore();
            textEditorCore.AppendText("123");

            // Action
            IImmutableRunList immutableRunList = textEditorCore.GetRunList(textEditorCore.GetAllDocumentSelection());

            // Assert
            // 可以返回一个包含空内容的列表
            Assert.AreEqual(1, immutableRunList.RunCount);
            Assert.AreEqual("123".Length, immutableRunList.CharCount);
        });

        "对空文本调用 GetRunList 方法，可以返回一个包含空内容的列表".Test(() =>
        {
            // Arrange
            var textEditorCore = TestHelper.GetTextEditorCore();

            // Action
            IImmutableRunList immutableRunList = textEditorCore.GetRunList(textEditorCore.GetAllDocumentSelection());

            // Assert
            // 可以返回一个包含空内容的列表
            Assert.AreEqual(0, immutableRunList.RunCount);
            Assert.AreEqual(0, immutableRunList.CharCount);
        });
    }
}