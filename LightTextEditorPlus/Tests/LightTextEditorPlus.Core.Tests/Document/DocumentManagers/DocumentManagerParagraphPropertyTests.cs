using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Document.Segments;
using LightTextEditorPlus.Core.Exceptions;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.TestsFramework;
using MSTest.Extensions.Contracts;

namespace LightTextEditorPlus.Core.Tests.Document.DocumentManagers;

/// <summary>
/// 文档管理的段落属性测试
/// </summary>
[TestClass()]
public class DocumentManagerParagraphPropertyTests
{
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
            textEditorCore.DocumentManager.SetStyleTextRunProperty< LayoutOnlyRunProperty>(_ => styleRunProperty);

            // 空文本空段，添加文本带属性
            IReadOnlyRunProperty runProperty = textEditorCore.CreateRunProperty(property => property with
            {
                FontSize = 12312,
                FontName = new FontName("Test")
            });
            textEditorCore.AppendRun(new TextRun(TestHelper.PlainNumberText, runProperty));
            // Assert
            // 添加之后段落属性变更为添加的文本的属性
            ParagraphProperty firstParagraphProperty = textEditorCore.DocumentManager.GetParagraphProperty(new ParagraphIndex(0));
            Assert.AreEqual(runProperty.FontSize, firstParagraphProperty.ParagraphStartRunProperty.FontSize);

            // 文本样式属性不会受到更改
            Assert.AreEqual(styleRunProperty.FontSize, textEditorCore.DocumentManager.StyleParagraphProperty.ParagraphStartRunProperty.FontSize);
        });
    }
}
