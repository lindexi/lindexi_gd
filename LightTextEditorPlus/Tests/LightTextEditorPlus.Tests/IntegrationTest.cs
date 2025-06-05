#nullable enable
using System.Diagnostics;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

using dotnetCampus.UITest.WPF;

using LightTextEditorPlus.Demo.Business.RichTextCases;

using MSTest.Extensions.Contracts;

using TextVisionComparer;

namespace LightTextEditorPlus.Tests;

[TestClass]
public class IntegrationTest
{
    [UIContractTestCase]
    public void RunIntegrationTest()
    {
        "执行集成测试".Test(async () =>
        {
            var provider = new IntegrationTestTextEditorProvider();
            var richTextCaseProvider = new RichTextCaseProvider(provider);

            var list = new List<(string Name, Exception Exception)>();

            foreach (IRichTextCase richTextCase in richTextCaseProvider.RichTextCases)
            {
                Console.WriteLine($"[IntegrationTest] Start {richTextCase.Name}");

                try
                {
                    using var context = TestFramework.CreateTextEditorInNewWindow();
                    var textEditor = context.TextEditor;
                    Assert.AreEqual(TextEditorSize, textEditor.Width, "集成测试下，文本一定是固定的尺寸");
                    Assert.AreEqual(TextEditorSize, textEditor.Height, "集成测试下，文本一定是固定的尺寸");

                    provider.TextEditor = textEditor;

                    // 一些初始化逻辑
                    textEditor.UseWpfLineSpacingStyle();

                    richTextCaseProvider.Run(richTextCase);

                    for (int i = 0; i < 5; i++)
                    {
                        await Task.Delay(10);
                        await context.TestWindow.Dispatcher.InvokeAsync(() =>
                        {
                        }, DispatcherPriority.Render);
                    }

                    var fileName = richTextCase.Name + ".png";

                    var imageFilePath = SaveAsImage(textEditor, fileName);
                    var assertImageFilePath = Path.Join(AppContext.BaseDirectory, "Assets", "TestImage", fileName);
                    if (File.Exists(assertImageFilePath))
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

                    if (TestFramework.IsDebug())
                    {
                        // 调试下，手动关闭
                        context.TestWindow.Close();
                    }
                }
                catch (Exception e)
                {
                    list.Add((richTextCase.Name, e));
                }
            }

            if (list.Count > 0)
            {
                throw new IntegrationTestException(list);
            }
        });
    }

#if DEBUG
    [UIContractTestCase]
#endif
    public void RunTestCase()
    {
        "调试下，执行一条测试内容".Test(async () =>
        {
            using var context = TestFramework.CreateTextEditorInNewWindow();
            var textEditor = context.TextEditor;

            var richTextCaseProvider = new RichTextCaseProvider(() => textEditor);

            richTextCaseProvider.Debug();

            await TestFramework.FreezeTestToDebug();
        });
    }

    private const int TextEditorSize = 600;

    public static string? CurrentTestFolder { get; set; }

    private static string SaveAsImage(TextEditor textEditor, string fileName)
    {
        RenderTargetBitmap renderTargetBitmap =
            new RenderTargetBitmap(TextEditorSize, TextEditorSize, 96, 96, PixelFormats.Pbgra32);
        renderTargetBitmap.Render(textEditor);

        PngBitmapEncoder pngBitmapEncoder = new PngBitmapEncoder();
        pngBitmapEncoder.Frames.Add(BitmapFrame.Create(renderTargetBitmap));

        if (CurrentTestFolder is null)
        {
            CurrentTestFolder = Path.Join(AppContext.BaseDirectory, "TestImage", Path.GetRandomFileName());
            Directory.CreateDirectory(CurrentTestFolder);
        }

        var folder = CurrentTestFolder;
        var file = Path.Join(folder, fileName);
        using FileStream fileStream = File.OpenWrite(file);
        pngBitmapEncoder.Save(fileStream);
        return file;
    }

    class IntegrationTestTextEditorProvider : ITextEditorProvider
    {
        public TextEditor TextEditor { get; set; } = null!;

        public TextEditor GetTextEditor()
        {
            return TextEditor;
        }
    }
}

public class VisionCompareResultException(string name, VisionCompareResult result, string assertImageFilePath, string imageFilePath) : Exception
{
    public override string ToString()
    {
        return $"""
                图片视觉对比失败
                测试用例: {name}
                对比结果: {result.Success}
                视觉相似:{result.IsSimilar()}
                视觉相似度: {result.SimilarityValue}
                像素数量:{result.PixelCount}
                对比的调试原因:{result.DebugReason}
                预设图片:{assertImageFilePath}
                当前状态截图:{imageFilePath}
                """;
    }
}

public class IntegrationTestException : AggregateException
{
    public IntegrationTestException(List<(string Name, Exception Exception)> exceptionList) : base(exceptionList.Select(t => t.Exception))
    {
        _exceptionList = exceptionList;
    }

    private readonly List<(string Name, Exception Exception)> _exceptionList;

    public override string Message => ToText();

    public override string ToString()
    {
        return ToText();
    }

    private string ToText()
    {
        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.AppendLine("IntegrationTest Fail!");
        stringBuilder.AppendLine($"Current Image Output Folder: {IntegrationTest.CurrentTestFolder}");

        stringBuilder.AppendLine("==========");

        foreach ((string name, Exception exception) in _exceptionList)
        {
            stringBuilder.AppendLine($"[IntegrationTest] Fail {name}")
                .AppendLine(exception.ToString());

            stringBuilder.AppendLine("==========");
        }

        return stringBuilder.ToString();
    }
}