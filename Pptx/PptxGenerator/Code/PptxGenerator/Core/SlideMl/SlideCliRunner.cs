using Avalonia.Media.Imaging;

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

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
                previewBitmap.Save(imagePath);
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

            return 0;
        }
        catch (OperationCanceledException)
        {
            Console.Error.WriteLine("操作已取消。\n");
            return 130;
        }
    }
}
