using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace PptxGenerator;

internal sealed class SlideCliRunner
{
    private readonly SlideChatManager _slideChatManager;

    public SlideCliRunner(SlideChatManager slideChatManager)
    {
        _slideChatManager = slideChatManager ?? throw new ArgumentNullException(nameof(slideChatManager));
    }

    public async Task<int> RunAsync(string prompt, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            Console.Error.WriteLine("请通过命令行传入一段描述文本。示例：dotnet run -- \"做一页介绍 SlideML 的幻灯片\"");
            return 1;
        }

        try
        {
            await _slideChatManager.SendSlideRequestAsync(prompt, cancellationToken).ConfigureAwait(false);

            var outputDirectory = Path.Join(Directory.GetCurrentDirectory(), "artifacts", "generated-slides");
            Directory.CreateDirectory(outputDirectory);

            var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            var xmlPath = Path.Join(outputDirectory, $"slide-{timestamp}.xml");
            var renderedXmlPath = Path.Join(outputDirectory, $"slide-{timestamp}.rendered.xml");
            var imagePath = Path.Join(outputDirectory, $"slide-{timestamp}.png");

            var currentSlideXml = _slideChatManager.CurrentSlideXml;
            var renderedXml = _slideChatManager.RenderedXml;
            var previewBitmap = _slideChatManager.PreviewBitmap;

            if (!string.IsNullOrWhiteSpace(currentSlideXml))
            {
                await File.WriteAllTextAsync(xmlPath, currentSlideXml, cancellationToken).ConfigureAwait(false);
                Console.WriteLine($"XML: {xmlPath}");
            }
            else
            {
                Console.WriteLine("警告：未生成 SlideML XML，可能模型未调用 render_slide 工具。");
            }

            if (!string.IsNullOrWhiteSpace(renderedXml))
            {
                await File.WriteAllTextAsync(renderedXmlPath, renderedXml, cancellationToken).ConfigureAwait(false);
                Console.WriteLine($"Rendered XML: {renderedXmlPath}");
            }

            if (previewBitmap is not null)
            {
                SaveBitmapAsPng(previewBitmap, imagePath);
                Console.WriteLine($"Preview Image: {imagePath}");
            }
            else
            {
                Console.WriteLine("警告：未生成预览图片，可能渲染过程出现错误。");
            }

            Console.WriteLine("生成完成");
            Console.WriteLine();

            Console.WriteLine("Warnings:");
            Console.WriteLine(string.IsNullOrWhiteSpace(_slideChatManager.WarningText) || _slideChatManager.WarningText == "(none)"
                ? "  (none)"
                : $"  {_slideChatManager.WarningText}");

            if (!string.IsNullOrWhiteSpace(currentSlideXml))
            {
                Console.WriteLine();
                Console.WriteLine("Final SlideML:");
                Console.WriteLine(currentSlideXml);
            }

            // 输出评估结果
            PrintEvaluation();

            return 0;
        }
        catch (OperationCanceledException)
        {
            Console.Error.WriteLine("操作已取消。\n");
            return 130;
        }
    }

    private void PrintEvaluation()
    {
        var slideEval = _slideChatManager.LastEvaluationResult;
        if (slideEval is { IsSuccess: true })
        {
            Console.WriteLine();
            Console.WriteLine("=== SlideML 评估报告 ===");
            Console.WriteLine($"综合评分: {slideEval.OverallScore:F1}/10");
            Console.WriteLine($"  XML 规范: {slideEval.XmlWellFormedness}/10");
            Console.WriteLine($"  布局结构: {slideEval.LayoutStructure}/10");
            Console.WriteLine($"  视觉平衡: {slideEval.VisualBalance}/10");
            Console.WriteLine($"  约束遵守: {slideEval.ConstraintAdherence}/10");
            Console.WriteLine($"  语义对齐: {slideEval.SemanticAlignment}/10");
            Console.WriteLine($"  美观度:   {slideEval.AestheticQuality}/10");
            Console.WriteLine();
            Console.WriteLine("改进建议:");
            foreach (var suggestion in slideEval.Suggestions)
            {
                Console.WriteLine($"  - {suggestion}");
            }
        }
        else if (slideEval is { IsSuccess: false })
        {
            Console.WriteLine();
            Console.WriteLine($"评估失败: {slideEval.ErrorMessage}");
        }
    }

    private static void SaveBitmapAsPng(BitmapSource bitmap, string filePath)
    {
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var fileStream = File.Create(filePath);
        encoder.Save(fileStream);
    }
}
