using Microsoft.Extensions.AI;

using System;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace PptxGenerator;

/// <summary>
/// 将 <see cref="SlideRenderPipeline.RenderAsync"/> 封装为 AI Tool，
/// 供模型在流式对话中自行调用并基于反馈修正。
/// </summary>
public sealed class SlideRenderTool
{
    private readonly SlideRenderPipeline _slideRenderer;

    /// <summary>
    /// 初始化 <see cref="SlideRenderTool"/> 的新实例。
    /// </summary>
    /// <param name="slideRenderer">渲染流水线。</param>
    public SlideRenderTool(SlideRenderPipeline slideRenderer)
    {
        _slideRenderer = slideRenderer ?? throw new ArgumentNullException(nameof(slideRenderer));
    }

    /// <summary>
    /// 渲染完成后触发，携带最新的 <see cref="LatestPreviewBitmap"/>。
    /// </summary>
    public event Action? SlideRendered;

    /// <summary>
    /// 渲染预览 Bitmap 缓存，供 UI 读取。
    /// </summary>
    public BitmapSource? LatestPreviewBitmap { get; private set; }

    /// <summary>
    /// 最近一次渲染的警告列表。
    /// </summary>
    public string LatestWarnings { get; private set; } = string.Empty;

    /// <summary>
    /// 最近一次渲染的错误列表。
    /// </summary>
    public string LatestErrors { get; private set; } = string.Empty;

    /// <summary>
    /// 最近一次渲染后回填的 XML。
    /// </summary>
    public string LatestRenderedXml { get; private set; } = string.Empty;

    /// <summary>
    /// 最近一次输入的 SlideML XML。
    /// </summary>
    public string LatestSlideXml { get; private set; } = string.Empty;

    /// <summary>
    /// 创建 SlideML 渲染 AI Tool。
    /// </summary>
    /// <returns>可用于 <see cref="ChatOptions.Tools"/> 的 AIFunction。</returns>
    public AIFunction CreateTool()
    {
        return AIFunctionFactory.Create(
            RenderSlideAsync,
            name: "render_slide",
            description: "将 SlideML XML 字符串渲染为页面预览图，返回回填后的 XML 与警告列表。"
                + " 生成 SlideML 后必须调用此工具验证排版效果。"
                + " 如果返回的警告不为空，请根据回填后的 XML 和警告修正问题并重新调用此工具验证。");
    }

    /// <summary>
    /// 创建获取渲染预览图的 AI Tool，返回 PNG 图片数据。
    /// 模型可以调用此工具"看到"渲染后的视觉效果，从而评估颜色、间距、对齐等。
    /// </summary>
    /// <returns>可用于 <see cref="ChatOptions.Tools"/> 的 AIFunction。</returns>
    public AIFunction CreatePreviewTool()
    {
        return AIFunctionFactory.Create(
            GetRenderPreviewAsync,
            name: "get_render_preview",
            description: "获取最近一次 render_slide 渲染的页面预览图（PNG 图片数据）。"
                + " 调用此工具可以查看渲染后的视觉效果，评估颜色搭配、间距、对齐等。"
                + " 仅在 render_slide 之后调用有效。");
    }

    [Description("获取最近一次渲染的页面预览图，返回 PNG 图片数据。")]
    private async Task<AIContent> GetRenderPreviewAsync()
    {
        var bitmap = LatestPreviewBitmap;
        if (bitmap is null)
        {
            return new TextContent("[get_render_preview] 错误：尚未渲染任何页面，请先调用 render_slide。");
        }

        using var memoryStream = new MemoryStream();
        SaveBitmapSourceAsPng(bitmap, memoryStream);
        memoryStream.Position = 0;
        return await DataContent.LoadFromAsync(memoryStream, "image/png").ConfigureAwait(false);
    }

    /// <summary>
    /// 获取最近一次渲染的预览图 <see cref="DataContent"/>，失败返回 null。
    /// 供调用方附加到 <see cref="SendMessageRequest.Contents"/> 中作为多模态输入。
    /// </summary>
    public async Task<DataContent?> CreatePreviewDataContentAsync(CancellationToken cancellationToken = default)
    {
        var bitmap = LatestPreviewBitmap;
        if (bitmap is null)
        {
            return null;
        }

        var memoryStream = new MemoryStream();
        SaveBitmapSourceAsPng(bitmap, memoryStream);
        memoryStream.Position = 0;
        return await DataContent.LoadFromAsync(memoryStream, "image/png", cancellationToken).ConfigureAwait(false);
    }

    [Description("将 SlideML XML 渲染为页面预览图，返回回填后的 XML 和警告列表。")]
    private async Task<string> RenderSlideAsync(
        [Description("SlideML 格式的 XML 字符串，根元素为 Page。")] string slideXml,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slideXml))
        {
            return "[render_slide] 错误：SlideML XML 不能为空。";
        }

        SlideRenderResult renderResult;
        try
        {
            renderResult = await _slideRenderer.RenderAsync(slideXml, cancellationToken);
        }
        catch (Exception ex)
        {
            return $"[render_slide] 渲染异常：{ex.Message}";
        }

        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            LatestPreviewBitmap = renderResult.PreviewBitmap;
            LatestWarnings = renderResult.Warnings.Count == 0
                ? "(none)"
                : string.Join("\n", renderResult.Warnings);
            LatestErrors = renderResult.Errors.Count == 0
                ? "(none)"
                : string.Join("\n", renderResult.Errors);
            LatestRenderedXml = renderResult.OutputXml;
            LatestSlideXml = renderResult.InputXml;
            SlideRendered?.Invoke();
        });

        var builder = new StringBuilder();
        builder.AppendLine("[render_slide] 渲染完成。");
        builder.AppendLine();
        builder.AppendLine("回填后的 XML：");
        builder.AppendLine(renderResult.OutputXml);
        builder.AppendLine();
        builder.AppendLine("警告列表：");
        if (renderResult.Warnings.Count == 0)
        {
            builder.AppendLine("(none)");
        }
        else
        {
            foreach (var warning in renderResult.Warnings)
            {
                builder.AppendLine($"- {warning}");
            }
        }

        builder.AppendLine();
        builder.AppendLine("错误列表：");
        if (renderResult.Errors.Count == 0)
        {
            builder.AppendLine("(none)");
        }
        else
        {
            foreach (var error in renderResult.Errors)
            {
                builder.AppendLine($"- {error}");
            }
        }

        return builder.ToString();
    }

    /// <summary>
    /// 将 <see cref="BitmapSource"/> 保存为 PNG 到流。
    /// </summary>
    private static void SaveBitmapSourceAsPng(BitmapSource bitmap, Stream stream)
    {
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        encoder.Save(stream);
    }
}
