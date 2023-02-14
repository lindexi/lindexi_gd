using System.Diagnostics;
using LightTextEditorPlus.Core.TestsFramework;
using MSTest.Extensions.Contracts;

namespace LightTextEditorPlus.Core.Tests;

[TestClass]
public class CaretOffsetTest
{
    [ContractTestCase]
    public void Foo()
    {
        "Foo".Test(() =>
        {
            EnumerationOptions enumOption = new EnumerationOptions
            {
                IgnoreInaccessible = true,
                ReturnSpecialDirectories = false,
                RecurseSubdirectories = false,
                AttributesToSkip = FileAttributes.Hidden | FileAttributes.System
            };

            foreach (string path in Directory.EnumerateFileSystemEntries("C:", "*", enumOption))
            {
                Debug.WriteLine(path);
            }
            Debug.WriteLine("=======");
            foreach (string path in Directory.EnumerateFileSystemEntries("C:\\", "*", enumOption))
            {
                Debug.WriteLine(path);
            }
        });
    }

    [ContractTestCase]
    public void TestCaretOffset()
    {
        "追加文本时，按照 DocumentChanging CurrentCaretOffsetChanging  CurrentCaretOffsetChanged 顺序触发".Test(() =>
        {
            // Arrange
            var textEditor = TestHelper.GetTextEditorCore();

            var count = 0;
            textEditor.DocumentChanging += (sender, args) =>
            {
                Assert.AreEqual(0, count);
                count++;
            };
            textEditor.CurrentCaretOffsetChanging += (sender, args) =>
            {
                Assert.AreEqual(1, count);
                count++;
            };
            textEditor.CurrentSelectionChanging += (sender, args) =>
            {
                Assert.AreEqual(2, count);
                count++;
            };
            textEditor.CurrentSelectionChanged += (sender, args) =>
            {
                Assert.AreEqual(3, count);
                count++;
            };
            textEditor.CurrentCaretOffsetChanged += (sender, args) =>
            {
                Assert.AreEqual(4, count);
                count++;
            };
            textEditor.DocumentChanged += (sender, args) =>
            {
                Assert.AreEqual(5, count);
                count++;
            };

            // Action
            textEditor.AppendText(TestHelper.PlainNumberText);

            // Assert
            Assert.AreEqual(6, count);
        });
    }
}