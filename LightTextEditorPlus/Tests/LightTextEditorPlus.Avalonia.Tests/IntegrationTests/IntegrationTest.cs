using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;

using LightTextEditorPlus.Demo.Business.RichTextCases;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LightTextEditorPlus.Tests.IntegrationTests;
using TextVisionComparer;

namespace LightTextEditorPlus.Avalonia.Tests.IntegrationTests;

[TestClass]
public class IntegrationTest
{
    [TestMethod]
    [DynamicData(nameof(AdditionData))]
    public async Task RunIntegrationTest(string testName, IRichTextCase richTextCase)
    {
        using TextEditTestContext context = TestFramework.CreateTextEditorInNewWindow();

        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            context.TestWindow.Title += $" {testName}";

            richTextCase.Exec(context.TextEditor);

            for (int i = 0; i < 10; i++)
            {
                await Task.Delay(10);
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    // 什么都不做，仅仅是为了让界面刷新
                }, DispatcherPriority.Render);
            }

            var fileName = testName + ".png";
            var imageFilePath = SaveAsImage(context.TextEditor, fileName);

            var assertImageFilePath = Path.Join(AppContext.BaseDirectory, "Assets", "TestImage", fileName);

            // 忽略的列表
            Span<string> ignoreList = 
            [
                // 随意的，每次都不同，不能加入测试
                "随意的字符属性",
                // 测试服务器不一定有这个字体
                "测试华文仿宋字体",
                // 依赖 Wingdings 2 字体，服务器不一定存在
                "无序项目符号",
            ];

            if (File.Exists(assertImageFilePath) &&!ignoreList.Contains(testName))
            {
                VisionComparer visionComparer = new VisionComparer();
                VisionCompareResult result = visionComparer.Compare(new FileInfo(assertImageFilePath), new FileInfo(imageFilePath));

                if (!result.Success || !result.IsSimilar())
                {
#if DEBUG
                    Debugger.Break();
#endif
                    await TestFramework.FreezeTestToDebug();
                    throw new VisionCompareResultException(richTextCase.Name, result, assertImageFilePath, imageFilePath);
                }
            }
        });

        await context.DebugWaitWindowClose();
    }

    public static IEnumerable<object[]> AdditionData
    {
        get
        {
            RichTextCaseProvider richTextCaseProvider = new RichTextCaseProvider(() => null!);
            HashSet<string> testNameHash = new HashSet<string>();
            foreach (IRichTextCase richTextCase in richTextCaseProvider.RichTextCases)
            {
                if (!testNameHash.Add(richTextCase.Name))
                {
                    throw new ArgumentException($"存在重复的测试名");
                }
            }

            return richTextCaseProvider.RichTextCases.Select(t => new object[]
            {
                t.Name,
                t
            });
        }
    }

    private const int TextEditorSize = 600;

    private static string SaveAsImage(TextEditor textEditor, string fileName)
    {
        // [Avalonia 已知问题 使用 RenderTargetBitmap 截图文本模糊 - lindexi - 博客园](https://www.cnblogs.com/lindexi/p/18978159 )
        Grid grid = (Grid) textEditor.Parent!;
        grid.Background = Brushes.White;

        using RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap(new PixelSize(TextEditorSize, TextEditorSize), new Vector(96, 96));
        renderTargetBitmap.Render(grid);
        var filePath = Path.Join(TestFramework.OutputDirectory.FullName, fileName);
        renderTargetBitmap.Save(filePath, 100);

        grid.Background = Brushes.Transparent;

        return filePath;
    }
}