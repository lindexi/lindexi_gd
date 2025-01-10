using LightTextEditorPlus.Core.Exceptions;
using LightTextEditorPlus.Core.TestsFramework;
using MSTest.Extensions.Contracts;

namespace LightTextEditorPlus.Core.Tests.Exceptions;

[TestClass]
public class TextEditorDirtyExceptionTest
{
    [ContractTestCase]
    public void ThrowException()
    {
        "在文本布局完成之前，获取文本布局信息，抛出 TextEditorDirtyException 异常".Test(() =>
        {
            // Arrange
            var testPlatformProvider = new TestPlatformProvider()
            .UseManuallyRequireLayoutDispatcher(out var manuallyRequireLayoutDispatcher);
            var textEditorCore = new TextEditorCore(testPlatformProvider);

            // Action
            // 对文本进行一些变更，默认情况下是空文本。空文本获取布局信息时，会执行空文本布局，这是符合预期的
            textEditorCore.AppendText(TestHelper.PlainNumberText);

            // Assert
            // 文本变更没布局，文本是脏的
            Assert.AreEqual(true, textEditorCore.IsDirty);
            Assert.IsNotNull(manuallyRequireLayoutDispatcher.CurrentLayoutAction,"文本变更之后，手动布局器可以收到布局请求");
            // 特意不让布局完成，直接获取布局信息。预期抛出异常
            Assert.ThrowsException<TextEditorDirtyException>(() =>
            {
                // 抛出 TextEditorDirtyException 异常
                textEditorCore.GetRenderInfo();
            });

            // 手动布局调度器执行布局一下，此时再获取布局信息，不应该抛出异常
            manuallyRequireLayoutDispatcher.InvokeLayoutAction();
            // 不用取值，不抛出异常就符合预期
            _ = textEditorCore.GetRenderInfo(); 

            //textEditorCore.DocumentChanging += (sender, args) =>
            //{
            //    // 在文本布局完成之前

            //    Assert.ThrowsException<TextEditorDirtyException>(() =>
            //    {
            //        // 抛出 TextEditorDirtyException 异常
            //        textEditorCore.GetRenderInfo();
            //    });
            //};
        });
    }
}
