using MSTest.Extensions.Contracts;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.TestsFramework;

namespace LightTextEditorPlus.Core.Tests.Primitive;

[TestClass]
public class TextLoggerTest
{
    [ContractTestCase]
    public void BuildTextLogger()
    {
        "文本的日志属性不为空，即使平台返回空".Test(() =>
        {
            // Arrange
            // 使用一定返回空的日志
            var testPlatformProvider = new EmptyLoggerTestPlatformProvider();

            // Action
            var textEditorCore = TestHelper.GetTextEditorCore(testPlatformProvider);

            // Assert
            Assert.IsNotNull(textEditorCore.Logger);
        });
    }

    class EmptyLoggerTestPlatformProvider : TestPlatformProvider
    {
        public override ITextLogger? BuildTextLogger() => null;
    }
}
