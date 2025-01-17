using LightTextEditorPlus.Core.TestsFramework;
using MSTest.Extensions.Contracts;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Document.UndoRedo;
using Moq;

namespace LightTextEditorPlus.Core.Tests.Document;

[TestClass()]
public class ChangeTextEditorStyleTextRunPropertyValueOperationTest
{
    [ContractTestCase()]
    public void SetStyleTextRunProperty()
    {
        "调用文本的 SetStyleTextRunProperty 方法修改默认文本字符属性，将会加入撤销重做任务，且可以通过撤销恢复机制进行撤销恢复".Test(() =>
        {
            // Arrange
            // 创建一个 Mock 类型，用来判断是否注入
            ChangeTextEditorStyleTextRunPropertyValueOperation? operation = null;
            var mock = new Mock<ITextEditorUndoRedoProvider>();
            mock.Setup(provider => provider
                    .Insert(It.IsNotNull<ChangeTextEditorStyleTextRunPropertyValueOperation>()))
                .Callback((ITextOperation t) =>
                {
                    operation = (ChangeTextEditorStyleTextRunPropertyValueOperation)t;
                });

            var textEditorCore = TestHelper.GetTextEditorCore(new FakeTestPlatformProvider(mock.Object));
            var oldValue = textEditorCore.DocumentManager.StyleRunProperty;

            // Action
            textEditorCore.DocumentManager.SetStyleTextRunProperty<LayoutOnlyRunProperty>(p => p with
            {
                FontSize = 15
            });
            var newValue = textEditorCore.DocumentManager.StyleRunProperty;

            // Assert
            mock.Verify(provider => provider
                    .Insert(It.IsNotNull<ChangeTextEditorStyleTextRunPropertyValueOperation>()),
                Times.Once);

            Assert.IsNotNull(operation);
            Assert.AreSame(oldValue, operation.OldValue);
            Assert.AreSame(newValue, operation.NewValue);

            // 测试撤销重做
            operation.Undo();
            Assert.AreEqual(operation.OldValue, textEditorCore.DocumentManager.StyleRunProperty);

            operation.Redo();
            Assert.AreEqual(operation.NewValue, textEditorCore.DocumentManager.StyleRunProperty);

            // 撤销恢复过程不会产生新的撤销恢复内容
            mock.Verify(
                provider => provider.Insert(It.IsNotNull<ChangeTextEditorStyleTextRunPropertyValueOperation>()),
                Times.Once);
        });

        "调用文本的 SetStyleTextRunProperty 方法修改默认文本字符属性，将会注入撤销恢复".Test(() =>
        {
            // Arrange
            // 创建一个 Mock 类型，用来判断是否注入
            var mock = new Mock<ITextEditorUndoRedoProvider>();

            var textEditorCore = TestHelper.GetTextEditorCore(new FakeTestPlatformProvider(mock.Object));

            // Action
            textEditorCore.DocumentManager.SetStyleTextRunProperty<LayoutOnlyRunProperty>(p => p with
            {
                FontSize = 15
            });

            // Assert
            mock.Verify(
                provider => provider.Insert(It.IsNotNull<ChangeTextEditorStyleTextRunPropertyValueOperation>()),
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
            return UndoRedoProvider!;
        }
    }
}
