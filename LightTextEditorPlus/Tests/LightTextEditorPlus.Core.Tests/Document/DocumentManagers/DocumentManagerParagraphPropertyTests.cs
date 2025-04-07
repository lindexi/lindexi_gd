using System.Diagnostics;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Document.Segments;
using LightTextEditorPlus.Core.Exceptions;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Rendering;
using LightTextEditorPlus.Core.TestsFramework;
using MSTest.Extensions.Contracts;

namespace LightTextEditorPlus.Core.Tests.Document.DocumentManagers;

/// <summary>
/// 文档管理的段落属性测试
/// </summary>
[TestClass()]
public class DocumentManagerParagraphPropertyTests
{
    /// <summary>
    /// 段落前后的属性设置测试
    /// </summary>
    [ContractTestCase]
    public void TestParagraphProperty_ParagraphBeforeAndAfter()
    {
        "文本有三段，全部都设置段落前后距离，可以正确布局".Test(() =>
        {
            // Arrange
            const double fontSize = 20;
            var textEditorCore = TestHelper.GetLayoutTestTextEditor(fontSize: fontSize);

            // Action
            // 全部都设置段落前后距离
            const double paragraphBefore = 15;
            const double paragraphAfter = 20;
            textEditorCore.DocumentManager.SetStyleParagraphProperty
            (
                textEditorCore.DocumentManager.StyleParagraphProperty with
                {
                    ParagraphBefore = paragraphBefore,
                    ParagraphAfter = paragraphAfter
                }
            );
            // 文本有三段
            textEditorCore.AppendText("""
                                      123
                                      123
                                      123
                                      """);
            // Assert
            Assert.IsFalse(textEditorCore.IsDirty, "此单元测试下为立刻布局，不会让文本是脏的");

            RenderInfoProvider renderInfoProvider = textEditorCore.GetRenderInfo();
            List<ParagraphRenderInfo> paragraphRenderInfoList = renderInfoProvider.GetParagraphRenderInfoList().ToList();

            IParagraphLayoutData firstParagraphLayoutData = paragraphRenderInfoList[0].ParagraphLayoutData;
            IParagraphLayoutData secondParagraphLayoutData = paragraphRenderInfoList[1].ParagraphLayoutData;
            IParagraphLayoutData thirdParagraphLayoutData = paragraphRenderInfoList[2].ParagraphLayoutData;

            // 特殊排版规则下，高度等于字高，方便测试
            Assert.AreEqual(fontSize, firstParagraphLayoutData.TextSize.Height);
            Assert.AreEqual(/*paragraphBefore + */fontSize + paragraphAfter, firstParagraphLayoutData.OutlineSize.Height,
                "首段的高度等于文本高度加上段后间距。为什么不加上段前间距？因为排版规则设置文本首段不添加段前间距");

            // 第二段紧接第一段
            Assert.AreEqual(firstParagraphLayoutData.OutlineSize.Height, secondParagraphLayoutData.OutlineBounds.Y, "第二段紧接第一段。第一段的 Bottom 和 高度 是相等的。第一段的 Top 就是 0 值");
            Assert.AreEqual(paragraphBefore + fontSize + paragraphAfter, secondParagraphLayoutData.OutlineSize.Height);

            Assert.AreEqual(secondParagraphLayoutData.OutlineBounds.Bottom, thirdParagraphLayoutData.StartPoint.ToCurrentArrangingTypePoint().Y);
            Assert.AreEqual(paragraphBefore + fontSize /*+ paragraphAfter*/, thirdParagraphLayoutData.OutlineSize.Height, "最后一段不加上段后间距");
        });
    }

    [ContractTestCase]
    public void TestSetStyleParagraphProperty()
    {
        "仅当文本没有创建出任何段落之前，初始化过程中，才能设置文本的样式字符属性".Test(() =>
        {
            // Arrange
            var textEditorCore = TestHelper.GetTextEditorCore();
            // Action
            // 初始化过程中
            // 能设置文本的样式字符属性
            // 没抛出异常就是成功
            FontName fontName = new FontName("Test");
            textEditorCore.DocumentManager.SetStyleTextRunProperty((LayoutOnlyRunProperty runProperty) =>
            {
                return runProperty with
                {
                    FontSize = 12312,
                    FontName = fontName
                };
            });

            // 尝试追加文本，让文本不在初始化状态
            textEditorCore.AppendText(TestHelper.PlainNumberText);

            // Assert
            // 预期此时抛出异常
            Assert.ThrowsException<InvalidOperationException>(() =>
            {
                textEditorCore.DocumentManager.SetStyleTextRunProperty((LayoutOnlyRunProperty runProperty) => runProperty with
                {
                    FontSize = 222,
                    FontName = new FontName("123")
                });
            });
        });
    }

    [ContractTestCase]
    public void TestStyleParagraphProperty()
    {
        "空文本空段，添加文本带属性，添加之后段落属性变更为添加的文本的属性".Test(() =>
        {
            // Arrange
            var textEditorCore = TestHelper.GetTextEditorCore();
            // Action
            var styleRunProperty = (LayoutOnlyRunProperty) textEditorCore.CreateRunProperty(property =>
                property with
                {
                    FontSize = 20
                }
            );
            textEditorCore.DocumentManager.SetStyleTextRunProperty<LayoutOnlyRunProperty>(_ => styleRunProperty);

            // 空文本空段，添加文本带属性
            IReadOnlyRunProperty runProperty = textEditorCore.CreateRunProperty(property => property with
            {
                FontSize = 12312,
                FontName = new FontName("Test")
            });
            textEditorCore.AppendRun(new TextRun(TestHelper.PlainNumberText, runProperty));
            // Assert
            // 添加之后段落属性变更为添加的文本的属性
            var firstParagraph = textEditorCore.DocumentManager.ParagraphManager.GetParagraph(new ParagraphIndex(0));
            Assert.AreEqual(runProperty.FontSize, firstParagraph.ParagraphStartRunProperty.FontSize);

            // 文本样式属性不会受到更改
            Assert.AreEqual(styleRunProperty.FontSize, textEditorCore.DocumentManager.StyleRunProperty.FontSize);
        });
    }
}
