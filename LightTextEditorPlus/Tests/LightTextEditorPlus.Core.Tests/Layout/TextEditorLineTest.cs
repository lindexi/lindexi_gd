using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Layout;
using LightTextEditorPlus.Core.Platform;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Rendering;
using LightTextEditorPlus.Core.TestsFramework;
using MSTest.Extensions.Contracts;

namespace LightTextEditorPlus.Core.Tests;

[TestClass]
public class TextEditorLineTest
{
    [ContractTestCase]
    public void TestLine()
    {
        "给定宽度为50的文本，输入123456字符串，采用15字号且定义字符宽度等于字号，可以布局出两行文本".Test(() =>
        {
            // Arrange
            // 使用特殊的平台定义，用来固定字符宽度等于字号。如此可以不受具体的系统环境和字体的影响
            var testPlatformProvider = new LineTestPlatformProvider(provider =>
            {
                // Assert
                var paragraphRenderInfos = provider.GetParagraphRenderInfoList().ToList();
                // 只有一段
                Assert.AreEqual(1, paragraphRenderInfos.Count);
                var paragraphLineRenderInfos = paragraphRenderInfos[0].GetLineRenderInfoList().ToList();
                // 可以布局出两行文本
                Assert.AreEqual(2, paragraphLineRenderInfos.Count);

                // 第一行将 “123” 布局进去。因为每个字符的宽度等于字号，也就是三个字符的总宽度等于 3*15=45 刚好放不下一个字符
                Assert.AreEqual(3, paragraphLineRenderInfos[0].Argument.CharList.Count);
                // 第二行将剩下的 “45” 布局进去
                Assert.AreEqual(2, paragraphLineRenderInfos[1].Argument.CharList.Count);
            });
            var textEditor = TestHelper.GetTextEditorCore(testPlatformProvider);

            // 给定宽度为50的文本
            textEditor.DocumentManager.DocumentWidth = 50;

            // Action
            textEditor.AppendText("12345");
        });
    }

    /// <summary>
    /// 专门给行测试的平台提供
    /// </summary>
    class LineTestPlatformProvider : TestPlatformProvider, IRenderManager
    {
        public LineTestPlatformProvider(Action<RenderInfoProvider>? renderAction = null)
        {
            RenderAction = renderAction;
        }

        public override ICharInfoMeasurer? GetCharInfoMeasurer()
        {
            return new LineTestCharInfoMeasurer();
        }

        public override IRenderManager? GetRenderManager()
        {
            return this;
        }

        public void Render(RenderInfoProvider renderInfoProvider)
        {
            RenderAction?.Invoke(renderInfoProvider);
        }

        private Action<RenderInfoProvider>? RenderAction { get; }
    }

    class LineTestCharInfoMeasurer : ICharInfoMeasurer
    {
        public CharInfoMeasureResult MeasureCharInfo(in CharInfo charInfo)
        {
            var bounds = new TextRect(0, 0, charInfo.RunProperty.FontSize, charInfo.RunProperty.FontSize);
            return new CharInfoMeasureResult(bounds);
        }
    }
}