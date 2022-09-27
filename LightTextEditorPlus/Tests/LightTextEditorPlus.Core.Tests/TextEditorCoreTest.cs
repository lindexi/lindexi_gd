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
}