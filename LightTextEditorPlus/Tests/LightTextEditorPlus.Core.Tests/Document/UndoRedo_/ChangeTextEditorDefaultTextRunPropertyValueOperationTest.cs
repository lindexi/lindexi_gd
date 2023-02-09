using LightTextEditorPlus.Core.TestsFramework;

using MSTest.Extensions.Contracts;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LightTextEditorPlus.Core.Document;
using Moq;

namespace LightTextEditorPlus.Core.Tests.Document;

[TestClass()]
public class ChangeTextEditorDefaultTextRunPropertyValueOperationTest
{
    [ContractTestCase()]
    public void SetDefaultTextRunProperty()
    {
        "调用文本的 SetDefaultTextRunProperty 方法修改默认文本字符属性，将会注入撤销恢复".Test(() =>
        {
            // Arrange
            // 创建一个 Mock 类型，用来判断是否注入
            var mock = new Mock<ITextEditorUndoRedoProvider>();

            var textEditorCore = TestHelper.GetTextEditorCore(new FakeTestPlatformProvider(mock.Object));

            // Action
            textEditorCore.DocumentManager.SetDefaultTextRunProperty<LayoutOnlyRunProperty>(p => p.FontSize = 15);

            // Assert
            mock.Verify(provider => provider.Insert(It.IsNotNull<ChangeTextEditorDefaultTextRunPropertyValueOperation>()),
                Times.Once);
        });
    }

    class FakeTestPlatformProvider : TestPlatformProvider
    {
        public FakeTestPlatformProvider(ITextEditorUndoRedoProvider undoRedoProvider)
        {
            UndoRedoProvider = undoRedoProvider;
        }

        public override ITextEditorUndoRedoProvider BuildTextEditorUndoRedoProvider()
        {
            return UndoRedoProvider;
        }

        public ITextEditorUndoRedoProvider UndoRedoProvider { get; }
    }
}
