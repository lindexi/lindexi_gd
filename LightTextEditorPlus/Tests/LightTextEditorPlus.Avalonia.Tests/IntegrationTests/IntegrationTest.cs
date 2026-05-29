using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;

using LightTextEditorPlus.Demo.Business.RichTextCases;
using SkiaSharp;

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
                "测试ad1b1c9-设置文本前景色导致连写字渲染失效",
                "测试037d9449-加粗的文本里下标渲染错误",
                "测试1d0299-使用 rr.导致字符宽度计算错误",
            ];

            if (ignoreList.Contains(testName))
            {
                //  忽略
            }
            else if (File.Exists(assertImageFilePath))
            {
                VisionComparer visionComparer = new VisionComparer();
                VisionCompareResult result = visionComparer.Compare(new FileInfo(assertImageFilePath),
                    new FileInfo(imageFilePath));

                if (!result.Success || !result.IsSimilar())
                {
#if DEBUG
                    if (!ReNewMode)
                    {
                        Debugger.Break();
                    }
#endif
                    Debug.WriteLine($"视觉识别存在差异，如符合预期，可将 '{imageFilePath}' 拷贝到 '{assertImageFilePath}' ");

                    if (ReNewMode)
                    {
                        CopyReNewFolder();
                    }
                    else
                    {
                        await TestFramework.FreezeTestToDebug();
                        throw new VisionCompareResultException(richTextCase.Name, result, assertImageFilePath,
                            imageFilePath);
                    }

                    // 绘制差异图片。差异图片是由四张图片组成的，按照两行两列的形式存放。前面一行，分别是原图、当前截图。后面一行，分别是原图和当前截图的差异圈选图，即在图片上方，根据 VisionCompareRect 给定范围，用红色框选出来
                    var outputFolder = Path.Join(AppContext.BaseDirectory, "Difference");
                    Directory.CreateDirectory(outputFolder);
                    var diffImageFile = Path.Join(outputFolder, fileName);
                    SaveDifferenceImage(assertImageFilePath, imageFilePath, result.CompareRectList, diffImageFile);
                }
            }
            else
            {
#if DEBUG
                Debugger.Break();
#endif

                Debug.WriteLine(
                    $"测试 '{testName}' 未找到对比图片 '{assertImageFilePath}'，已将测试结果保存到 '{imageFilePath}'，请确认是否符合预期，如符合预期，可将其拷贝到 '{assertImageFilePath}' ");
                CopyReNewFolder();
            }

            void CopyReNewFolder()
            {
                var outputFolder = Path.Join(AppContext.BaseDirectory, "VisionDifference");
                Directory.CreateDirectory(outputFolder);
                var newAssertImageFilePath = Path.Join(outputFolder, fileName);
                File.Copy(imageFilePath, newAssertImageFilePath, true);
            }
        });

        if (ReNewMode)
        {
            context.CloseTestWindow();
            // 更新模式下，就不要等待调试了，直接结束测试就行了
            return;
        }

        await context.DebugWaitWindowClose();
    }

    /// <summary>
    /// 重新更新的模式
    /// </summary>
    private static bool ReNewMode { get; } 
    // 代码审查注意，这个参数必须是 false 值
        = false;

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

    private static void SaveDifferenceImage(string assertImageFilePath, string imageFilePath,
        IReadOnlyList<VisionCompareRect> compareRectList, string diffImageFilePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(assertImageFilePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(imageFilePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(diffImageFilePath);
        ArgumentNullException.ThrowIfNull(compareRectList);

        using SKBitmap assertBitmap = SKBitmap.Decode(assertImageFilePath)
                                     ?? throw new InvalidOperationException($"无法读取基准图片 '{assertImageFilePath}'");
        using SKBitmap currentBitmap = SKBitmap.Decode(imageFilePath)
                                      ?? throw new InvalidOperationException($"无法读取当前截图 '{imageFilePath}'");

        int cellWidth = Math.Max(assertBitmap.Width, currentBitmap.Width);
        int cellHeight = Math.Max(assertBitmap.Height, currentBitmap.Height);

        using SKBitmap diffBitmap = new SKBitmap(cellWidth * 2, cellHeight * 2, assertBitmap.ColorType,
            assertBitmap.AlphaType, assertBitmap.ColorSpace);
        using SKCanvas canvas = new SKCanvas(diffBitmap);
        canvas.Clear(SKColors.White);

        DrawImage(canvas, assertBitmap, 0, 0);
        DrawImage(canvas, currentBitmap, cellWidth, 0);
        DrawImage(canvas, assertBitmap, 0, cellHeight);
        DrawImage(canvas, currentBitmap, cellWidth, cellHeight);

        using SKPaint borderPaint = new SKPaint
        {
            Color = SKColors.Red,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2,
            IsAntialias = true
        };

        foreach (VisionCompareRect compareRect in compareRectList)
        {
            SKRect rect = CreateRect(compareRect);
            //canvas.DrawRect(rect, borderPaint);

            rect.Offset(0, cellHeight);
            canvas.DrawRect(rect, borderPaint);

            rect.Offset(cellWidth, 0);
            canvas.DrawRect(rect, borderPaint);
        }

        using SKImage image = SKImage.FromBitmap(diffBitmap);
        using SKData data = image.Encode(SKEncodedImageFormat.Png, 100);
        using FileStream fileStream = File.Open(diffImageFilePath, FileMode.Create, FileAccess.Write, FileShare.Read);
        data.SaveTo(fileStream);

        static void DrawImage(SKCanvas canvas, SKBitmap bitmap, float x, float y)
        {
            canvas.DrawBitmap(bitmap, x, y);
        }

        static SKRect CreateRect(VisionCompareRect compareRect)
        {
            return SKRect.Create(compareRect.X, compareRect.Y, compareRect.Width, compareRect.Height);
        }
    }
}