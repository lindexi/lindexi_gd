using Avalonia.Media.Imaging;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PptxGenerator;

internal sealed class SlideCliRunner
{
    private readonly SlideGenerationService _slideGenerationService;

    public SlideCliRunner(SlideGenerationService slideGenerationService)
    {
        _slideGenerationService = slideGenerationService ?? throw new ArgumentNullException(nameof(slideGenerationService));
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
            var result = await _slideGenerationService.GenerateAsync(prompt, cancellationToken).ConfigureAwait(false);
            var outputDirectory = Path.Combine(Directory.GetCurrentDirectory(), "artifacts", "generated-slides");
            Directory.CreateDirectory(outputDirectory);

            var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            var xmlPath = Path.Combine(outputDirectory, $"slide-{timestamp}.xml");
            var renderedXmlPath = Path.Combine(outputDirectory, $"slide-{timestamp}.rendered.xml");
            var imagePath = Path.Combine(outputDirectory, $"slide-{timestamp}.png");

            await File.WriteAllTextAsync(xmlPath, result.FinalSlideXml, cancellationToken).ConfigureAwait(false);
            await File.WriteAllTextAsync(renderedXmlPath, result.FinalRenderResult.OutputXml, cancellationToken).ConfigureAwait(false);
            result.FinalRenderResult.PreviewBitmap.Save(imagePath);

            Console.WriteLine("生成完成");
            Console.WriteLine($"XML: {xmlPath}");
            Console.WriteLine($"Rendered XML: {renderedXmlPath}");
            Console.WriteLine($"Preview Image: {imagePath}");
            Console.WriteLine();

            Console.WriteLine("Warnings:");
            if (result.FinalRenderResult.Warnings.Count == 0)
            {
                Console.WriteLine("  (none)");
            }
            else
            {
                foreach (var warning in result.FinalRenderResult.Warnings)
                {
                    Console.WriteLine($"  {warning}");
                }
            }

            Console.WriteLine();
            Console.WriteLine("Final SlideML:");
            Console.WriteLine(result.FinalSlideXml);
            return 0;
        }
        catch (OperationCanceledException)
        {
            Console.Error.WriteLine("操作已取消。\n");
            return 130;
        }
    }
}
