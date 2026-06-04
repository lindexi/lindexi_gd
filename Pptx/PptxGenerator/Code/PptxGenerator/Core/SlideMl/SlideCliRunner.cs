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

            await File.WriteAllTextAsync(xmlPath, _slideChatManager.CurrentSlideXml, cancellationToken).ConfigureAwait(false);
            await File.WriteAllTextAsync(renderedXmlPath, _slideChatManager.RenderedXml, cancellationToken).ConfigureAwait(false);
            _slideChatManager.PreviewBitmap?.Save(imagePath);

            Console.WriteLine("生成完成");
            Console.WriteLine($"XML: {xmlPath}");
            Console.WriteLine($"Rendered XML: {renderedXmlPath}");
            Console.WriteLine($"Preview Image: {imagePath}");
            Console.WriteLine();

            Console.WriteLine("Warnings:");
            Console.WriteLine(string.IsNullOrWhiteSpace(_slideChatManager.WarningText) || _slideChatManager.WarningText == "(none)"
                ? "  (none)"
                : $"  {_slideChatManager.WarningText}");

            Console.WriteLine();
            Console.WriteLine("Final SlideML:");
            Console.WriteLine(_slideChatManager.CurrentSlideXml);
            return 0;
        }
        catch (OperationCanceledException)
        {
            Console.Error.WriteLine("操作已取消。\n");
            return 130;
        }
    }
}
