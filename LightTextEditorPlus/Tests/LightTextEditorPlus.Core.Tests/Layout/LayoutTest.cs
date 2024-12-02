using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Exceptions;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Rendering;
using LightTextEditorPlus.Core.TestsFramework;

using MSTest.Extensions.Contracts;

namespace LightTextEditorPlus.Core.Tests.Layout;

[TestClass]
public class LayoutTest
{
    [ContractTestCase]
    public void LayoutParagraph()
    {
        "文本包含两段，布局过一次，在第一段进行追加，第二段只需修改坐标不需要重新布局".Test(() =>
        {
            var renderManagerTestPlatformProvider = new RenderManagerTestPlatformProvider();
            var textEditorCore = TestHelper.GetTextEditorCore(renderManagerTestPlatformProvider);

            // 加入两段文本，用于测试
            textEditorCore.AppendText("abc\r\ndef");

            // 先给他一个缓存数据，这样就可以知道第二段是不是重新布局了
            var renderInfoProvider = textEditorCore.GetRenderInfo();
            var paragraphRenderInfoList = renderInfoProvider.GetParagraphRenderInfoList().ToList();
            Assert.AreEqual(2, paragraphRenderInfoList.Count);
            // 获取第二段的第一行。第二段其实也只有一行
            // 在这一行里面设置缓存。如果后续第二段不需要重新布局，那就依然能获取到第二段的第一行的缓存
            var lineList = paragraphRenderInfoList[1].GetLineRenderInfoList().ToList();
            object cache = new object();
            lineList[0].SetDrawnResult(new LineDrawnResult(cache));

            // 在第一段进行追加，第二段只需修改坐标不需要重新布局
            textEditorCore.EditAndReplaceRun(new TextRun("1"), new Selection(new CaretOffset(3), 0));

            // 预期没有重新布局第二段，也就是放入第二段的缓存没有被干掉
            renderInfoProvider = textEditorCore.GetRenderInfo();
            lineList = renderInfoProvider.GetParagraphRenderInfoList().ToList()[1].GetLineRenderInfoList().ToList();
            Assert.AreSame(cache, lineList[0].Argument.LineAssociatedRenderData);
        });
    }

    [ContractTestCase]
    public void ChangeDocumentOnUpdatingLayout()
    {
        $"在布局的过程中，修改了文档内容，将会抛出 {nameof(ChangeDocumentOnUpdatingLayoutException)} 错误".Test(() =>
        {
            var testWholeLineLayouter = new TestWholeLineLayouter();
            var testPlatformProvider = new TestPlatformProvider()
            {
                WholeLineLayouter = testWholeLineLayouter
            };
            var textEditorCore = TestHelper.GetTextEditorCore(testPlatformProvider);

            // 配置这个 WholeLineLayouter 将会在布局的时候，修改文档内容
            testWholeLineLayouter.LayoutWholeLineFunc = _ =>
            {
                // 其实在这里就炸了
                textEditorCore.AppendText(TestHelper.PlainNumberText);

                return default;
            };

            // 修改文本，触发布局，然后布局过程触发修改文本
            Assert.ThrowsException<ChangeDocumentOnUpdatingLayoutException>(() =>
            {
                textEditorCore.AppendText(TestHelper.PlainNumberText);
            });
        });
    }

    [ContractTestCase]
    public void LayoutTwoParagraph()
    {
        "分两次追加，第一次追加 `a\\r\\n` 第二次追加 `b` 内容，可以排版出两段两行".Test(() =>
        {
            var testPlatformProvider = new RenderManagerTestPlatformProvider();
            var testRenderManager = testPlatformProvider.TestRenderManager;

            var textEditorCore = TestHelper.GetTextEditorCore(testPlatformProvider);

            textEditorCore.AppendText("a\r\n");

            // 追加的是 a\r\n 导致出现两段
            Assert.IsNotNull(testRenderManager.CurrentRenderInfoProvider);
            var paragraphRenderInfoList = testRenderManager.CurrentRenderInfoProvider.GetParagraphRenderInfoList().ToList();
            Assert.AreEqual(2, paragraphRenderInfoList.Count);

            // 第二次追加，预期可以排版
            textEditorCore.AppendText("b");

            var provider = testRenderManager.CurrentRenderInfoProvider;
            var nextParagraphRenderInfoList = provider.GetParagraphRenderInfoList().ToList();
            Assert.AreEqual(2, nextParagraphRenderInfoList.Count);
            var paragraphLineList1 = nextParagraphRenderInfoList[0].GetLineRenderInfoList().ToList();
            Assert.AreEqual(1, paragraphLineList1.Count);
            Assert.AreEqual("a", paragraphLineList1[0].Argument.CharList[0].CharObject.ToText());

            var paragraphLineList2 = nextParagraphRenderInfoList[1].GetLineRenderInfoList().ToList();
            Assert.AreEqual(1, paragraphLineList2.Count);
            Assert.AreEqual("b", paragraphLineList2[0].Argument.CharList[0].CharObject.ToText());
        });
    }

    [ContractTestCase]
    public void TestParagraphBefore()
    {
        "文本包含段前段后距离，可以给文本计算入段前段后距离".Test(() =>
        {
            // Arrange
            var textEditorCore = TestHelper.GetTextEditorCore(new FixCharSizePlatformProvider())
                // 固定行距，用于减少行距影响，只测试段落前后距离
                .UseFixedLineSpacing();

            // Action
            var paragraphProperty = textEditorCore.DocumentManager.GetParagraphProperty(0);
            textEditorCore.DocumentManager.SetParagraphProperty(0, paragraphProperty with
            {
                // 随便定义两个距离，又刚好不是整数，方便测试
                ParagraphBefore = 21,
                ParagraphAfter = 22
            });

            textEditorCore.AppendText("a\nb");

            // Assert
            // 加入有两段，那么总尺寸应该是，根据首段不加段前，末段不加段后
            // a
            // \nb
            // 文档尺寸 = a 字符高度 15 + a 段后 22 + b 段前 21 + b 字符高度 15
            TextRect documentLayoutBounds = textEditorCore.GetDocumentLayoutBounds();
            Assert.AreEqual(15 + 22 + 21 + 15, documentLayoutBounds.Height);
        });
    }

    [ContractTestCase]
    public void TestEmptyParagraph()
    {
        "空段文本包含段前段后距离，可以给空段文本计算入段前段后距离".Test(() =>
        {
            // Arrange
            var textEditorCore = TestHelper.GetTextEditorCore(new FixCharSizePlatformProvider())
                // 固定行距，用于减少行距影响，只测试段落前后距离
                .UseFixedLineSpacing();

            // Action
            var paragraphProperty = textEditorCore.DocumentManager.GetParagraphProperty(0);
            textEditorCore.DocumentManager.SetParagraphProperty(0, paragraphProperty with
            {
                // 随便定义两个距离，又刚好不是整数，方便测试
                ParagraphBefore = 21,
                ParagraphAfter = 22
            });

            textEditorCore.AppendText("a\n\nb");

            // Assert
            // 加入有两段，那么总尺寸应该是，根据首段不加段前，末段不加段后
            // a
            // \n
            // \nb
            // 文档尺寸 = a 字符高度 15 + a 段后 21 + 空段段前 21 + 空段高度 15 + 空段段后 22 + b 段前 22 + b 字符高度 15
            TextRect documentLayoutBounds = textEditorCore.GetDocumentLayoutBounds();
            Assert.AreEqual(15 + 22 + 21 + 15 + 22 + 21 + 15, documentLayoutBounds.Height);
        });

        "空段文本包含段后距离，可以给空段文本计算入段后距离".Test(() =>
        {
            // Arrange
            var textEditorCore = TestHelper.GetTextEditorCore(new FixCharSizePlatformProvider())
                // 固定行距，用于减少行距影响，只测试段落前后距离
                .UseFixedLineSpacing();

            // Action
            var paragraphProperty = textEditorCore.DocumentManager.GetParagraphProperty(0);
            textEditorCore.DocumentManager.SetParagraphProperty(0, paragraphProperty with
            {
                // 随便定义两个距离，又刚好不是整数，方便测试
                ParagraphBefore = 5,
                ParagraphAfter = 22
            });

            // 空段放在首段，根据首段不计算段前距离，即可让段落只计算段后距离
            textEditorCore.AppendText("\na");

            // Assert
            // 文档尺寸 = 空段高度 15 + 空段段后 22 + a段前 5 + a段高度 15
            TextRect documentLayoutBounds = textEditorCore.GetDocumentLayoutBounds();
            Assert.AreEqual(15 + 22 + 5 + 15, documentLayoutBounds.Height);
        });

        "空段文本包含段前距离，可以给空段文本计算入段前距离".Test(() =>
        {
            // Arrange
            var textEditorCore = TestHelper.GetTextEditorCore(new FixCharSizePlatformProvider())
                // 固定行距，用于减少行距影响，只测试段落前后距离
                .UseFixedLineSpacing();

            // Action
            var paragraphProperty = textEditorCore.DocumentManager.GetParagraphProperty(0);
            textEditorCore.DocumentManager.SetParagraphProperty(0, paragraphProperty with
            {
                // 随便定义两个距离，又刚好不是整数，方便测试
                ParagraphBefore = 5,
                ParagraphAfter = 22
            });

            // 空段放在末段，根据末段不计算段末距离，即可让段落只计算段前距离
            textEditorCore.AppendText("a\n");

            // Assert
            // 文档尺寸 = a段高度 15 + a段段后 22 + 空段前 5 + 空段高度 15
            TextRect documentLayoutBounds = textEditorCore.GetDocumentLayoutBounds();
            Assert.AreEqual(15 + 22 + 5 + 15, documentLayoutBounds.Height);
        });
    }
}