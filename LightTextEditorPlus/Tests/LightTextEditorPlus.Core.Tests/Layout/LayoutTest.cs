using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Document.Segments;
using LightTextEditorPlus.Core.Exceptions;
using LightTextEditorPlus.Core.Layout;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Primitive.Collections;
using LightTextEditorPlus.Core.Rendering;
using LightTextEditorPlus.Core.TestsFramework;

using MSTest.Extensions.AssertExtensions;
using MSTest.Extensions.Contracts;

namespace LightTextEditorPlus.Core.Tests.Layout;

[TestClass]
public class LayoutTest
{
    [ContractTestCase]
    public void GuidingLayoutInfoTest()
    {
        "获取指导布局信息时，空文本至少表现为一段一行且行内无字符".Test(() =>
        {
            TextEditorCore textEditorCore = TestHelper.GetLayoutTestTextEditor();

            GuidingLayoutInfo guidingLayoutInfo = textEditorCore.GetCurrentGuidingLayoutInfo();

            Assert.AreEqual(1, guidingLayoutInfo.ParagraphCount);
            Assert.AreEqual(1, guidingLayoutInfo.LineCount);
            Assert.AreEqual(0, guidingLayoutInfo.CharCount);
            Assert.AreEqual(1, guidingLayoutInfo.ParagraphList.Count);
            Assert.AreEqual(1, guidingLayoutInfo.ParagraphList[0].LineCount);
            Assert.AreEqual(0, guidingLayoutInfo.ParagraphList[0].CharCount);
            Assert.AreEqual(0, guidingLayoutInfo.ParagraphList[0].LineList[0].CharCount);
        });

        "获取指导布局信息时，可以覆盖空段、多段、多行、符号、宽度和对齐组合场景".Test(() =>
        {
            TextEditorCore textEditorCore = TestHelper.GetLayoutTestTextEditor();
            textEditorCore.AppendText("\n12,34\n\nABCDE12345\n符号!?（）AB");

            textEditorCore.DocumentManager.SetParagraphProperty(new ParagraphIndex(1),
                textEditorCore.DocumentManager.StyleParagraphProperty with
                {
                    HorizontalTextAlignment = HorizontalTextAlignment.Center
                });
            textEditorCore.DocumentManager.SetParagraphProperty(new ParagraphIndex(3),
                textEditorCore.DocumentManager.StyleParagraphProperty with
                {
                    HorizontalTextAlignment = HorizontalTextAlignment.Right
                });

            GuidingLayoutInfo guidingLayoutInfo = textEditorCore.GetCurrentGuidingLayoutInfo();

            Assert.AreEqual(5, guidingLayoutInfo.ParagraphCount);
            Assert.AreEqual(7, guidingLayoutInfo.LineCount);
            CollectionAssert.AreEqual(new[] { 1, 1, 1, 2, 2 }, guidingLayoutInfo.ParagraphList.Select(t => t.LineCount).ToArray());
            CollectionAssert.AreEqual(new[] { 0, 5, 0, 10, 8 }, guidingLayoutInfo.ParagraphList.Select(t => t.CharCount).ToArray());
            CollectionAssert.AreEqual(new[] { 0, 5, 0, 5, 5, 4, 4 }, guidingLayoutInfo.ParagraphList.SelectMany(t => t.LineList).Select(t => t.CharCount).ToArray());

            GuidingParagraphLayoutInfo centerParagraph = guidingLayoutInfo.ParagraphList[1];
            Assert.IsTrue(centerParagraph.LineList[0].ContentStartPoint.X > centerParagraph.LineList[0].StartPoint.X);

            GuidingParagraphLayoutInfo rightParagraph = guidingLayoutInfo.ParagraphList[3];
            Assert.IsTrue(rightParagraph.LineList[0].ContentStartPoint.X > centerParagraph.LineList[0].ContentStartPoint.X);

            GuidingLineLayoutInfo shortLine = guidingLayoutInfo.ParagraphList[1].LineList[0];
            Assert.IsTrue(shortLine.ContentBounds.Width > 0);
        });

        "获取指导布局信息时，文本宽度大于给定文本框宽度时会被布局为多行".Test(() =>
        {
            TextEditorCore textEditorCore = TestHelper.GetLayoutTestTextEditor();
            textEditorCore.AppendText("123456");

            GuidingLayoutInfo guidingLayoutInfo = textEditorCore.GetCurrentGuidingLayoutInfo();
            Assert.IsTrue(guidingLayoutInfo.LineCount > 1);
        });

        "获取指导布局信息时，可以覆盖竖排和蒙文竖排场景".Test(() =>
        {
            TextEditorCore textEditorCore = TestHelper.GetLayoutTestTextEditor();
            textEditorCore.DocumentManager.DocumentHeight = TestHelper.LayoutTestFontSize * 3 + 0.1;
            textEditorCore.AppendText("甲乙丙丁\nABCD");

            textEditorCore.ArrangingType = ArrangingType.Vertical;
            GuidingLayoutInfo verticalGuidingLayoutInfo = textEditorCore.GetCurrentGuidingLayoutInfo();
            Assert.AreEqual(ArrangingType.Vertical, verticalGuidingLayoutInfo.ArrangingType);
            Assert.AreEqual(4, verticalGuidingLayoutInfo.LineCount);
            CollectionAssert.AreEqual(new[] { 2, 2 }, verticalGuidingLayoutInfo.ParagraphList.Select(t => t.LineCount).ToArray());
            Assert.IsTrue(verticalGuidingLayoutInfo.ParagraphList[0].LineList[1].StartPoint.X < verticalGuidingLayoutInfo.ParagraphList[0].LineList[0].StartPoint.X);

            textEditorCore.ArrangingType = ArrangingType.Mongolian;
            GuidingLayoutInfo mongolianGuidingLayoutInfo = textEditorCore.GetCurrentGuidingLayoutInfo();
            Assert.AreEqual(ArrangingType.Mongolian, mongolianGuidingLayoutInfo.ArrangingType);
            Assert.AreEqual(4, mongolianGuidingLayoutInfo.LineCount);
            Assert.IsTrue(mongolianGuidingLayoutInfo.ParagraphList[0].LineList[1].StartPoint.X > mongolianGuidingLayoutInfo.ParagraphList[0].LineList[0].StartPoint.X);
        });

        "设置指导布局信息之后，将优先按照指导布局的行字符数进行布局".Test(() =>
        {
            TextEditorCore sourceTextEditor = TestHelper.GetLayoutTestTextEditor();
            sourceTextEditor.AppendText("123456789");
            GuidingLayoutInfo guidingLayoutInfo = sourceTextEditor.GetCurrentGuidingLayoutInfo();

            TextEditorCore targetTextEditor = TestHelper.GetLayoutTestTextEditor(lineCharCount: 10, fontSize: 10);
            targetTextEditor.DocumentManager.DocumentWidth = sourceTextEditor.DocumentManager.DocumentWidth;
            targetTextEditor.AppendText("123456789");

            bool isSet = targetTextEditor.SetGuidingLayoutInfoForNextUpdateLayout(guidingLayoutInfo);

            Assert.IsTrue(isSet);

            GuidingLayoutInfo appliedGuidingLayoutInfo = targetTextEditor.GetCurrentGuidingLayoutInfo();
            CollectionAssert.AreEqual(
                guidingLayoutInfo.ParagraphList.SelectMany(t => t.LineList).Select(t => t.CharCount).ToArray(),
                appliedGuidingLayoutInfo.ParagraphList.SelectMany(t => t.LineList).Select(t => t.CharCount).ToArray());
        });

        "设置无效的指导布局信息时，将记录日志并回退默认布局".Test(() =>
        {
            var logger = new Primitive.TextLoggerTest.TestTextLogger();
            var testPlatformProvider = new TestPlatformProvider()
            {
                TextLogger = logger
            };
            testPlatformProvider.UsingFixedCharSizeCharInfoMeasurer();
            testPlatformProvider.UseFakeLineSpacingCalculator();

            TextEditorCore textEditorCore = TestHelper.GetLayoutTestTextEditor(testPlatformProvider: testPlatformProvider);
            textEditorCore.AppendText("12345");
            GuidingLayoutInfo guidingLayoutInfo = textEditorCore.GetCurrentGuidingLayoutInfo();

            GuidingLayoutInfo invalidGuidingLayoutInfo = guidingLayoutInfo with
            {
                ParagraphList =
                [
                    guidingLayoutInfo.ParagraphList[0] with
                    {
                        LineList = [guidingLayoutInfo.ParagraphList[0].LineList[0] with { CharCount = 4 }]
                    }
                ]
            };

            bool isSet = textEditorCore.SetGuidingLayoutInfoForNextUpdateLayout(invalidGuidingLayoutInfo);

            Assert.IsTrue(isSet);
            GuidingLayoutInfo appliedGuidingLayoutInfo = textEditorCore.GetCurrentGuidingLayoutInfo();
            CollectionAssert.AreEqual(
                guidingLayoutInfo.ParagraphList.SelectMany(t => t.LineList).Select(t => t.CharCount).ToArray(),
                appliedGuidingLayoutInfo.ParagraphList.SelectMany(t => t.LineList).Select(t => t.CharCount).ToArray());
            Assert.AreEqual(1, logger.WarningList.Count);
        });
    }

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
            Assert.ThrowsExactly<ChangeDocumentOnUpdatingLayoutException>(() =>
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
        "文本包含段前段后间距，可以给文本计算入段前段后间距".Test(() =>
        {
            // Arrange
            var textEditorCore = TestHelper.GetTextEditorCore(new FixCharSizePlatformProvider())
                // 固定行距，用于减少行距影响，只测试段落前后间距
                .UseFixedLineSpacing();

            // Action
            var paragraphProperty = textEditorCore.DocumentManager.GetParagraphProperty(new ParagraphIndex(0));
            textEditorCore.DocumentManager.SetParagraphProperty(new ParagraphIndex(0), paragraphProperty with
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
            TextRect documentLayoutBounds = textEditorCore.GetDocumentLayoutBounds().DocumentContentBounds;
            Assert.AreEqual(15 + 22 + 21 + 15, documentLayoutBounds.Height);
        });
    }

    [ContractTestCase]
    public void TestEmptyParagraph()
    {
        "横排的空段文本的 ParagraphLayoutData.OutlineBounds 也会包含整个文档的横排宽度，高度为空段文本高度".Test(() =>
        {
            // Arrange
            var textEditorCore = TestHelper.GetLayoutTestTextEditor();

            // Action
            textEditorCore.AppendText("123\n\nabc");

            // Assert
            RenderInfoProvider renderInfoProvider = textEditorCore.GetRenderInfo();
            // 一共有三段，其中中间一段是空段
            var paragraphRenderList = renderInfoProvider.GetParagraphRenderInfoList();
            Assert.AreEqual(3, paragraphRenderList.Count);
            // 预期每一段的 OutlineBounds 尺寸都是相同的
            var firstParagraph = paragraphRenderList[0];

            for (int i = 1; i < paragraphRenderList.Count; i++)
            {
                ParagraphRenderInfo paragraphRenderInfo = paragraphRenderList[i];
                Assert.AreEqual(firstParagraph.ParagraphLayoutData.OutlineBounds.TextSize,
                    paragraphRenderInfo.ParagraphLayoutData.OutlineBounds.TextSize);
            }
        });

        "空段文本包含段前段后间距，可以给空段文本计算入段前段后间距".Test(() =>
        {
            // Arrange
            var textEditorCore = TestHelper.GetTextEditorCore(new FixCharSizePlatformProvider().UseManuallyRequireLayoutDispatcher(out var dispatcher))
                // 固定行距，用于减少行距影响，只测试段落前后间距
                .UseFixedLineSpacing();

            // Action
            var paragraphProperty = textEditorCore.DocumentManager.GetParagraphProperty(new ParagraphIndex(0));
            textEditorCore.DocumentManager.SetParagraphProperty(new ParagraphIndex(0), paragraphProperty with
            {
                // 随便定义两个距离，又刚好不是整数，方便测试
                ParagraphBefore = 21,
                ParagraphAfter = 22
            });

            textEditorCore.AppendText("a\n\nb");

            // 开始布局。防止准备过程进入太多次
            dispatcher.InvokeLayoutAction();

            // Assert
            // 加入有两段，那么总尺寸应该是，根据首段不加段前，末段不加段后
            // a
            // \n
            // \nb
            // 文档尺寸 = a 字符高度 15 + a 段后 22 + 空段段前 21 + 空段高度 15 + 空段段后 22 + b 段前 22 + b 字符高度 15
            TextRect documentLayoutBounds = textEditorCore.GetDocumentLayoutBounds().DocumentContentBounds;
            Assert.AreEqual(15 + 22 + 21 + 15 + 22 + 21 + 15, documentLayoutBounds.Height);
        });

        "空段文本包含段后间距，可以给空段文本计算入段后间距".Test(() =>
        {
            // Arrange
            var textEditorCore = TestHelper.GetTextEditorCore(new FixCharSizePlatformProvider())
                // 固定行距，用于减少行距影响，只测试段落前后间距
                .UseFixedLineSpacing();

            // Action
            var paragraphProperty = textEditorCore.DocumentManager.GetParagraphProperty(new ParagraphIndex(0));
            textEditorCore.DocumentManager.SetParagraphProperty(new ParagraphIndex(0), paragraphProperty with
            {
                // 随便定义两个间距，又刚好不是整数，方便测试
                ParagraphBefore = 5,
                ParagraphAfter = 22
            });

            // 空段放在首段，根据首段不计算段前间距，即可让段落只计算段后间距
            textEditorCore.AppendText("\na");

            // Assert
            // 文档尺寸 = 空段高度 15 + 空段段后 22 + a段前 5 + a段高度 15
            TextRect documentLayoutBounds = textEditorCore.GetDocumentLayoutBounds().DocumentContentBounds;
            Assert.AreEqual(15 + 22 + 5 + 15, documentLayoutBounds.Height);
        });

        "空段文本包含段前间距，可以给空段文本计算入段前间距".Test(() =>
        {
            // Arrange
            var textEditorCore = TestHelper.GetTextEditorCore(new FixCharSizePlatformProvider())
                // 固定行距，用于减少行距影响，只测试段落前后间距
                .UseFixedLineSpacing();

            // Action
            var paragraphProperty = textEditorCore.DocumentManager.GetParagraphProperty(new ParagraphIndex(0));
            textEditorCore.DocumentManager.SetParagraphProperty(new ParagraphIndex(0), paragraphProperty with
            {
                // 随便定义两个距离，又刚好不是整数，方便测试
                ParagraphBefore = 5,
                ParagraphAfter = 22
            });

            // 空段放在末段，根据末段不计算段末间距，即可让段落只计算段前间距
            textEditorCore.AppendText("a\n");

            // Assert
            // 文档尺寸 = a段高度 15 + a段段后 22 + 空段前 5 + 空段高度 15
            TextRect documentLayoutBounds = textEditorCore.GetDocumentLayoutBounds().DocumentContentBounds;
            Assert.AreEqual(15 + 22 + 5 + 15, documentLayoutBounds.Height);
        });
    }

    [ContractTestCase]
    public void TestCenterHorizontalTextAlignment()
    {
        "文本设置为水平居中，可以获取比水平居左更大的段落内容范围".Test(() =>
        {
            // Arrange
            const double fontSize = TestHelper.LayoutTestFontSize;
            TextEditorCore testTextEditor = TestHelper.GetLayoutTestTextEditor(fontSize: fontSize);

            // Action
            var text = "123";
            testTextEditor.AppendText(text);

            ParagraphRenderInfo paragraphRenderInfo = testTextEditor.GetRenderInfo().GetParagraphRenderInfoList().First();
            TextRect paragraphContentBounds = paragraphRenderInfo.ParagraphLayoutData.TextContentBounds;
            Assert.AreEqual(fontSize * text.Length, paragraphContentBounds.Width, "水平居左时，段落尺寸宽度刚好就是三个字符乘以每个字符的宽度的值");

            // 修改为水平居中的情况
            testTextEditor.DocumentManager.SetParagraphProperty(new ParagraphIndex(0), testTextEditor.DocumentManager.StyleParagraphProperty with
            {
                HorizontalTextAlignment = HorizontalTextAlignment.Center
            });

            paragraphRenderInfo = testTextEditor.GetRenderInfo().GetParagraphRenderInfoList().First();
            paragraphContentBounds = paragraphRenderInfo.ParagraphLayoutData.TextContentBounds;
            // Assert
            // 水平居中之后，原本一行只能放入 5 个字符，现在放入了 3 个，居中之后，就约等于前后各空一个字符的宽度
            Assert.IsTrue(Math.Abs(paragraphContentBounds.Width - (text.Length + 1/*前面空一个字符*/) * fontSize) < 0.1);
        });

        "文本设置为水平居中，可以获取比水平居左更大的文档内容范围".Test(() =>
        {
            // Arrange
            const double fontSize = TestHelper.LayoutTestFontSize;
            TextEditorCore testTextEditor = TestHelper.GetLayoutTestTextEditor(fontSize: fontSize);

            // Action
            var text = "123";
            testTextEditor.AppendText(text);
            // 获取居左的内容尺寸
            TextRect documentContentBounds = testTextEditor.GetDocumentLayoutBounds().DocumentContentBounds;
            Assert.AreEqual(fontSize * text.Length, documentContentBounds.Width, "水平居左时，文档尺寸宽度刚好就是三个字符乘以每个字符的宽度的值");

            // 修改为水平居中的情况
            testTextEditor.DocumentManager.SetParagraphProperty(new ParagraphIndex(0), testTextEditor.DocumentManager.StyleParagraphProperty with
            {
                HorizontalTextAlignment = HorizontalTextAlignment.Center
            });

            // Assert
            // 水平居中之后，原本一行只能放入 5 个字符，现在放入了 3 个，居中之后，就约等于前后各空一个字符的宽度
            documentContentBounds = testTextEditor.GetDocumentLayoutBounds().DocumentContentBounds;
            Assert.IsTrue(Math.Abs(documentContentBounds.Width - (text.Length + 1/*前面空一个字符*/) * fontSize) < 0.1);
        });
    }
}
