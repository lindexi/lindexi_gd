using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Document.Segments;
using LightTextEditorPlus.Core.Platform;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.TestsFramework;

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
            var textEditorCore = TestHelper.GetTextEditorCore();

            // 没有异常，那就是符合预期
            Assert.IsNotNull(textEditorCore);
        });
    }

    [ContractTestCase]
    public void GetHitParagraphData()
    {
        "命中到文本的段落的换车符，可以自动修改为命中段末".Test(() =>
        {
            // Arrange
            var textEditorCore = TestHelper.GetTextEditorCore();
            // 加上一个测试信息，用来有两个段落，这样才能命中
            textEditorCore.AppendText("12\r\n");

            // Action
            var caretOffset = new CaretOffset(textEditorCore.CurrentCaretOffset.Offset - 1);
            var paragraphManager = textEditorCore.DocumentManager.ParagraphManager;
            var result = paragraphManager.GetHitParagraphData(caretOffset);

            var hitCharData = result.GetHitCharData();
            Assert.IsNotNull(hitCharData);

            // Assert
            Assert.AreEqual(2, result.HitOffset.Offset);
            // 命中到 '2' 字符
            Assert.AreEqual("2", hitCharData.CharObject.ToText());
        });
    }

    [ContractTestCase]
    public void EventArrange()
    {
        "文本编辑的事件触发是 DocumentChanging DocumentChanged LayoutCompleted 顺序".Test(() =>
        {
            // Arrange
            var textEditorCore = TestHelper.GetTextEditorCore();
            var raiseCount = 0;

            textEditorCore.DocumentChanging += (sender, args) =>
            {
                // Assert
                Assert.AreEqual(0, raiseCount);
                raiseCount++;
            };

            textEditorCore.DocumentChanged += (sender, args) =>
            {
                // Assert
                Assert.AreEqual(1, raiseCount);
                raiseCount = 2;
            };

            textEditorCore.LayoutCompleted += (sender, args) =>
            {
                // Assert
                Assert.AreEqual(2, raiseCount);
                raiseCount = 3;
            };

            // Action
            textEditorCore.AppendText(TestHelper.PlainNumberText);

            // Assert
            Assert.AreEqual(3, raiseCount);
        });
    }

    [ContractTestCase]
    public void GetDocumentBounds()
    {
        "给文本编辑器追加一段纯文本，在布局渲染完成之后，可以获取到文档的尺寸".Test(() =>
        {
            // Arrange
            var textEditorCore = TestHelper.GetTextEditorCore();

            textEditorCore.LayoutCompleted += (sender, args) =>
            {
                // Assert
                // 可以获取到文档的内容尺寸
                var documentBounds = textEditorCore.GetDocumentLayoutBounds().DocumentContentBounds;
                Assert.AreEqual(true, documentBounds.Width > 0);
                Assert.AreEqual(true, documentBounds.Height > 0);
            };

            // Action
            textEditorCore.AppendText(TestHelper.PlainNumberText);
        });
    }

    [ContractTestCase]
    public void TestDebugName()
    {
        "给文本添加字符串内容，可以自动生成文本的调试名".Test(() =>
        {
            // Arrange
            var textEditorCore = TestHelper.GetTextEditorCore();

            // Action
            textEditorCore.AppendText(TestHelper.PlainNumberText);

            // Assert
            Assert.IsNotNull(textEditorCore.DebugName);
        });
    }

    [ContractTestCase]
    public void SetInDebugMode()
    {
        "调用 SetInDebugMode 可以设置文本进入调试模式".Test(() =>
        {
            // Arrange
            var textEditorCore = TestHelper.GetTextEditorCore();

            // Action
            textEditorCore.SetInDebugMode();

            // Assert
            Assert.AreEqual(true, textEditorCore.IsInDebugMode);
        });
    }

    [ContractTestCase]
    public void UpdateLayout()
    {
        "布局过的文本设置 自适应模式 属性，将触发布局".Test(() =>
        {
            // Arrange
            var count = 0;
            var testPlatformProvider = new TestPlatformProvider();
            testPlatformProvider.RequireDispatchUpdateLayoutHandler = action =>
            {
                // 触发布局
                if (count == 0)
                {
                    action();
                }

                count++;
            };
            var textEditorCore = TestHelper.GetTextEditorCore(testPlatformProvider);
            textEditorCore.AppendText(TestHelper.PlainNumberText);

            // Action
            textEditorCore.SizeToContent = TextSizeToContent.Height;

            // Assert
            // 触发布局，一次是初始化时，一次是设置属性
            Assert.AreEqual(2, count);
        });

        "布局过的文本设置 多倍行距呈现策略 属性，将触发布局".Test(() =>
        {
            // Arrange
            var count = 0;
            var testPlatformProvider = new TestPlatformProvider();
            testPlatformProvider.RequireDispatchUpdateLayoutHandler = action =>
            {
                // 触发布局
                if (count == 0)
                {
                    action();
                }
                count++;
            };
            var textEditorCore = TestHelper.GetTextEditorCore(testPlatformProvider);
            textEditorCore.AppendText(TestHelper.PlainNumberText);

            // Action
            textEditorCore.LineSpacingStrategy = LineSpacingStrategy.FirstLineShrink;

            // Assert
            // 触发布局，一次是初始化时，一次是设置属性
            Assert.AreEqual(2, count);
        });

        "布局过的文本设置 行距算法 属性，将触发布局".Test(() =>
        {
            // Arrange
            var count = 0;
            var testPlatformProvider = new TestPlatformProvider();
            testPlatformProvider.RequireDispatchUpdateLayoutHandler = action =>
            {
                // 触发布局
                if (count == 0)
                {
                    action();
                }
                count++;
            };
            var textEditorCore = TestHelper.GetTextEditorCore(testPlatformProvider);
            textEditorCore.AppendText(TestHelper.PlainNumberText);

            // Action
            textEditorCore.LineSpacingAlgorithm = LineSpacingAlgorithm.WPF;

            // Assert
            // 触发布局，一次是初始化时，一次是设置属性
            Assert.AreEqual(2, count);
        });

        "布局过的文本设置 ArrangingType 属性，将触发布局".Test(() =>
        {
            // Arrange
            var count = 0;
            var testPlatformProvider = new TestPlatformProvider();
            testPlatformProvider.RequireDispatchUpdateLayoutHandler = action =>
            {
                // 触发布局
                if (count == 0)
                {
                    action();
                }
                count++;
            };
            var textEditorCore = TestHelper.GetTextEditorCore(testPlatformProvider);
            textEditorCore.AppendText(TestHelper.PlainNumberText);

            // Action
            textEditorCore.ArrangingType = ArrangingType.Vertical;

            // Assert
            // 触发布局，一次是初始化时，一次是设置属性
            Assert.AreEqual(2, count);
        });

        "空文本初始化时设置 自适应模式 属性，不会触发布局".Test(() =>
        {
            // Arrange
            var count = 0;
            var testPlatformProvider = new TestPlatformProvider();
            testPlatformProvider.RequireDispatchUpdateLayoutHandler = _ =>
            {
                // 触发布局
                count++;
            };
            var textEditorCore = TestHelper.GetTextEditorCore(testPlatformProvider);
            Assert.AreEqual(0, count);

            // Action
            textEditorCore.SizeToContent = TextSizeToContent.Height;

            // Assert
            // 不会触发布局
            Assert.AreEqual(0, count);
        });

        "空文本初始化时设置 多倍行距呈现策略 属性，不会触发布局".Test(() =>
        {
            // Arrange
            var count = 0;
            var testPlatformProvider = new TestPlatformProvider();
            testPlatformProvider.RequireDispatchUpdateLayoutHandler = _ =>
            {
                // 触发布局
                count++;
            };
            var textEditorCore = TestHelper.GetTextEditorCore(testPlatformProvider);
            Assert.AreEqual(0, count);

            // Action
            textEditorCore.LineSpacingStrategy = LineSpacingStrategy.FirstLineShrink;

            // Assert
            // 不会触发布局
            Assert.AreEqual(0, count);
        });

        "空文本初始化时设置 行距算法 属性，不会触发布局".Test(() =>
        {
            // Arrange
            var count = 0;
            var testPlatformProvider = new TestPlatformProvider();
            testPlatformProvider.RequireDispatchUpdateLayoutHandler = _ =>
            {
                // 触发布局
                count++;
            };
            var textEditorCore = TestHelper.GetTextEditorCore(testPlatformProvider);
            Assert.AreEqual(0, count);

            // Action
            textEditorCore.LineSpacingAlgorithm = LineSpacingAlgorithm.WPF;

            // Assert
            // 不会触发布局
            Assert.AreEqual(0, count);
        });

        "空文本初始化时设置 ArrangingType 属性，不会触发布局".Test(() =>
        {
            // Arrange
            var count = 0;
            var testPlatformProvider = new TestPlatformProvider();
            testPlatformProvider.RequireDispatchUpdateLayoutHandler = _ =>
            {
                // 触发布局
                count++;
            };
            var textEditorCore = TestHelper.GetTextEditorCore(testPlatformProvider);
            Assert.AreEqual(0, count);

            // Action
            textEditorCore.ArrangingType = ArrangingType.Vertical;

            // Assert
            // 不会触发布局
            Assert.AreEqual(0, count);
        });
    }
}