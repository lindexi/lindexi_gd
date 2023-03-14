using dotnetCampus.UITest.WPF;

using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Primitive;

using MSTest.Extensions.Contracts;

namespace LightTextEditorPlus.Tests.Document;

[TestClass()]
public class RunPropertyTests
{
    [UIContractTestCase]
    public void EqualsTest()
    {
        "设置两个字符的字符属性的字体名不相同，可以返回是两个不同的字符属性".Test(() =>
        {
            // Arrange
            using var context = TestFramework.CreateTextEditorInNewWindow();
            var textEditor = context.TextEditor;
            // 放入两个字符，用来测试
            textEditor.TextEditorCore.AppendText("12");

            // Action
            // 设置两个字符的字符属性的字体名不相同
            textEditor.SetRunProperty(property => property.FontName = new FontName("A"), new Selection(new CaretOffset(0), 1));
            textEditor.SetRunProperty(property => property.FontName = new FontName("B"), new Selection(new CaretOffset(1), 1));

            // Assert
            var runPropertyList = textEditor.TextEditorCore.DocumentManager.GetRunPropertyRange(new Selection(new CaretOffset(0), 2)).ToList();

            Assert.AreEqual(2, runPropertyList.Count);

            Assert.AreEqual(false, runPropertyList[0].Equals(runPropertyList[1]));

            Assert.AreNotEqual(runPropertyList[0], runPropertyList[1]);
        });
    }
}