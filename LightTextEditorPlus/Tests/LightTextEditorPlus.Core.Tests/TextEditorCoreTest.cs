using MSTest.Extensions.Contracts;

namespace LightTextEditorPlus.Core.Tests;

[TestClass]
public class TextEditorCoreTest
{
    [ContractTestCase]
    public void TestCreate()
    {
        "测试文本的创建".Test(() =>
        {
            var textEditorCore = new TextEditorCore(new TestPlatformProvider());

            // 没有异常，那就是符合预期
            Assert.IsNotNull(textEditorCore);
        });
    }


    [ContractTestCase]
    public void BuildTextLogger()
    {
        "文本的日志属性不为空，即使平台返回空".Test(() =>
        {
            // Arrange
            var testPlatformProvider = new TestPlatformProvider();

            // Action
            var textEditorCore = new TextEditorCore(testPlatformProvider);

            // Assert
            Assert.IsNotNull(textEditorCore.Logger);
        });
    }
}