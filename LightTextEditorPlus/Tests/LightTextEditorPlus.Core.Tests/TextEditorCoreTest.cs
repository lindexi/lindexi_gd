﻿using LightTextEditorPlus.Core.Platform;
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
                // 可以获取到文档的尺寸
                var documentBounds = textEditorCore.GetDocumentLayoutBounds();
                Assert.AreEqual(true, documentBounds.Width > 0);
                Assert.AreEqual(true, documentBounds.Height > 0);
            };

            // Action
            textEditorCore.AppendText(TestHelper.PlainNumberText);
        });
    }

    [ContractTestCase]
    public void AppendText()
    {
        "给文本编辑器连续两次追加文本，可以将后追加的文本，追加在最后".Test(() =>
        {
            // Arrange
            var textEditorCore = TestHelper.GetTextEditorCore();

            // Action
            textEditorCore.AppendText("123");
            textEditorCore.AppendText("456");

            // Assert
            var renderInfoProvider = textEditorCore.GetRenderInfo();
            Assert.IsNotNull(renderInfoProvider);

            var paragraphRenderInfoList = renderInfoProvider.GetParagraphRenderInfoList().ToList();

            // 可以排版出来1段1行
            Assert.AreEqual(1, paragraphRenderInfoList.Count);

            Assert.AreEqual("123456", paragraphRenderInfoList.First().GetLineRenderInfoList().First().LineLayoutData.GetText());
        });

        @"给文本编辑器追加 123\r\n123\r\n 文本，可以排版出来三段".Test(() =>
        {
            // Arrange
            var textEditorCore = TestHelper.GetTextEditorCore();

            textEditorCore.LayoutCompleted += (sender, args) =>
            {
                // Assert
                var renderInfoProvider = textEditorCore.GetRenderInfo();
                Assert.IsNotNull(renderInfoProvider);

                var paragraphRenderInfoList = renderInfoProvider.GetParagraphRenderInfoList().ToList();

                // 可以排版出来三段
                Assert.AreEqual(3, paragraphRenderInfoList.Count);
            };

            // Action
            textEditorCore.AppendText("123\r\n123\r\n");
        });

        "给文本编辑器追加两段纯文本，可以排版出来两段两行".Test(() =>
        {
            // Arrange
            var textEditorCore = TestHelper.GetTextEditorCore();

            textEditorCore.LayoutCompleted += (sender, args) =>
            {
                // Assert
                var renderInfoProvider = textEditorCore.GetRenderInfo();
                Assert.IsNotNull(renderInfoProvider);

                var paragraphRenderInfoList = renderInfoProvider.GetParagraphRenderInfoList().ToList();

                // 可以排版出来两段两行
                Assert.AreEqual(2, paragraphRenderInfoList.Count);

                foreach (var paragraphRenderInfo in paragraphRenderInfoList)
                {
                    var paragraphLineRenderInfoList = paragraphRenderInfo.GetLineRenderInfoList().ToList();
                    Assert.AreEqual(1, paragraphLineRenderInfoList.Count);

                    Assert.AreEqual("123", paragraphLineRenderInfoList[0].LineLayoutData.GetText());
                }
            };

            // Action
            // 给文本编辑器追加两段纯文本
            textEditorCore.AppendText("123\r\n123");
        });

        "给文本编辑器追加一段纯文本，先触发 DocumentChanging 再触发 DocumentChanged 事件".Test(() =>
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

            // Action
            textEditorCore.AppendText(TestHelper.PlainNumberText);

            // Assert
            Assert.AreEqual(2, raiseCount);
        });
    }


    [ContractTestCase]
    public void AppendBreakParagraph()
    {
        "给文本追加一个 \\r\\n 字符串，文本可以分两段".Test(() =>
        {
            // Arrange
            var textEditorCore = TestHelper.GetTextEditorCore(new FixCharSizePlatformProvider());

            // Action
            textEditorCore.AppendText("\r\n");

            // Assert
            Assert.AreEqual(0, textEditorCore.GetDocumentLayoutBounds().Width);
            Assert.AreEqual(30, textEditorCore.GetDocumentLayoutBounds().Height);
        });
    }
}