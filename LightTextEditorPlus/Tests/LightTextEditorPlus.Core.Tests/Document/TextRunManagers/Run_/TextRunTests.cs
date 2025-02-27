using LightTextEditorPlus.Core.Document;
using MSTest.Extensions.Contracts;

namespace LightTextEditorPlus.Core.Tests.Document.TextRunManagers;

[TestClass()]
public class TextRunTests
{
    [ContractTestCase()]
    public void SplitAtTest()
    {
        "给定字符串为 123456 的 TextRun 对象，从第 3 个开始分割，再将分割获取的第一个 Run 从 1 开始分割，可以分割为 1 和 23 两个 Run 对象".Test(() =>
        {
            // Arrange
            // 给定字符串为 123456 的 TextRun 对象
            var run = new TextRun("123456");

            // Action
            // 从第 3 个开始分割
            var (firstRun, secondRun) = run.SplitAt(3);
            // 再将分割获取的第一个 Run 从 1 开始分割
            (firstRun, secondRun) = firstRun.SplitAt(1);

            // Assert
            // 可以分割为 1 和 23 两个 Run 对象
            Assert.AreEqual(1, firstRun.Count);
            Assert.AreEqual(2, secondRun.Count);

            Assert.AreEqual("1", firstRun.GetChar(0).ToText());

            Assert.AreEqual("2", secondRun.GetChar(0).ToText());
            Assert.AreEqual("3", secondRun.GetChar(1).ToText());
        });

        "给定字符串为 123456 的 TextRun 对象，从第 3 个开始分割，可以分割为 123 和 456 两个 Run 对象".Test(() =>
        {
            // Arrange
            // 给定字符串为 123456 的 TextRun 对象
            var run = new TextRun("123456");

            // Action
            // 从第 3 个开始分割
            var (firstRun, secondRun) = run.SplitAt(3);

            // Assert
            // 可以分割为 123 和 456 两个 Run 对象
            Assert.AreEqual(3, firstRun.Count);
            Assert.AreEqual(3, secondRun.Count);

            Assert.AreEqual("1", firstRun.GetChar(0).ToText());
            Assert.AreEqual("2", firstRun.GetChar(1).ToText());
            Assert.AreEqual("3", firstRun.GetChar(2).ToText());

            Assert.AreEqual("4", secondRun.GetChar(0).ToText());
            Assert.AreEqual("5", secondRun.GetChar(1).ToText());
            Assert.AreEqual("6", secondRun.GetChar(2).ToText());
        });
    }
}