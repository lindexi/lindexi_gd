using LightTextEditorPlus.Core.TestsFramework;
using MSTest.Extensions.Contracts;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Document.UndoRedo;
using Moq;

namespace LightTextEditorPlus.Core.Tests.Document;

[TestClass()]
public class ChangeTextEditorDefaultTextRunPropertyValueOperationTest
{
    [ContractTestCase()]
    public void SetDefaultTextRunProperty()
    {
        "调用文本的 SetDefaultTextRunProperty 方法修改默认文本字符属性，将会加入撤销重做任务，且可以通过撤销恢复机制进行撤销恢复".Test(() =>
        {
            // Arrange
            // 创建一个 Mock 类型，用来判断是否注入
            ChangeTextEditorDefaultTextRunPropertyValueOperation? operation = null;
            var mock = new Mock<ITextEditorUndoRedoProvider>();
            mock.Setup(provider => provider
                    .Insert(It.IsNotNull<ChangeTextEditorDefaultTextRunPropertyValueOperation>()))
                .Callback((ITextOperation t) =>
                {
                    operation = (ChangeTextEditorDefaultTextRunPropertyValueOperation)t;
                });

            var textEditorCore = TestHelper.GetTextEditorCore(new FakeTestPlatformProvider(mock.Object));
            var oldValue = textEditorCore.DocumentManager.CurrentRunProperty;

            // Action
            textEditorCore.DocumentManager.SetDefaultTextRunProperty<LayoutOnlyRunProperty>(p => p with
            {
                FontSize = 15
            });
            var newValue = textEditorCore.DocumentManager.CurrentRunProperty;

            // Assert
            mock.Verify(provider => provider
                    .Insert(It.IsNotNull<ChangeTextEditorDefaultTextRunPropertyValueOperation>()),
                Times.Once);

            Assert.IsNotNull(operation);
            Assert.AreSame(oldValue, operation.OldValue);
            Assert.AreSame(newValue, operation.NewValue);

            // 测试撤销重做
            operation.Undo();
            Assert.AreEqual(operation.OldValue, textEditorCore.DocumentManager.CurrentRunProperty);

            operation.Redo();
            Assert.AreEqual(operation.NewValue, textEditorCore.DocumentManager.CurrentRunProperty);

            // 撤销恢复过程不会产生新的撤销恢复内容
            mock.Verify(
                provider => provider.Insert(It.IsNotNull<ChangeTextEditorDefaultTextRunPropertyValueOperation>()),
                Times.Once);
        });

        "调用文本的 SetDefaultTextRunProperty 方法修改默认文本字符属性，将会注入撤销恢复".Test(() =>
        {
            // Arrange
            // 创建一个 Mock 类型，用来判断是否注入
            var mock = new Mock<ITextEditorUndoRedoProvider>();

            var textEditorCore = TestHelper.GetTextEditorCore(new FakeTestPlatformProvider(mock.Object));

            // Action
            textEditorCore.DocumentManager.SetDefaultTextRunProperty<LayoutOnlyRunProperty>(p => p with
            {
                FontSize = 15
            });

            // Assert
            mock.Verify(
                provider => provider.Insert(It.IsNotNull<ChangeTextEditorDefaultTextRunPropertyValueOperation>()),
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