using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PptxGenerator;

/// <summary>
/// SlideML CLI 运行器，处理命令行模式下的幻灯片生成流程。
/// </summary>
public sealed class SlideCliRunner
{
    private readonly SlideChatManager _slideChatManager;

    /// <summary>
    /// 初始化 <see cref="SlideCliRunner"/> 的新实例。
    /// </summary>
    /// <param name="slideChatManager">SlideML 聊天管理器。</param>
    public SlideCliRunner(SlideChatManager slideChatManager)
    {
        _slideChatManager = slideChatManager ?? throw new ArgumentNullException(nameof(slideChatManager));
    }

    /// <summary>
    /// 运行 CLI 模式，生成幻灯片并保存到文件。
    /// </summary>
    /// <param name="prompt">用户自然语言需求描述。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>退出码（0 成功，1 失败，130 取消）。</returns>
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
            var previewImage = _slideChatManager.PreviewImage;

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

            if (previewImage is not null)
            {
                previewImage.Save(imagePath);
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
}