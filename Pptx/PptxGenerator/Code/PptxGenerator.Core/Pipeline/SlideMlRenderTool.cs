using System.ComponentModel;
using System.Text;
using AgentLib;
using Microsoft.Extensions.AI;
using PptxGenerator.Models;
using PptxGenerator.Rendering;

namespace PptxGenerator.Pipeline;

/// <summary>
/// 将 <see cref="ISlideMlRenderPipeline.RenderAsync"/> 封装为 AI Tool，
/// 供模型在流式对话中自行调用并基于反馈修正。
/// </summary>
public sealed class SlideMlRenderTool
{
    private readonly ISlideMlRenderPipeline _renderPipeline;
    private readonly IMainThreadDispatcher _dispatcher;

    /// <summary>
    /// 初始化 <see cref="SlideMlRenderTool"/> 的新实例。
    /// </summary>
    /// <param name="renderPipeline">SlideML 渲染管道。</param>
    /// <param name="dispatcher">主线程调度器。</param>
    public SlideMlRenderTool(ISlideMlRenderPipeline renderPipeline, IMainThreadDispatcher dispatcher)
    {
        _renderPipeline = renderPipeline ?? throw new ArgumentNullException(nameof(renderPipeline));
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
    }

    /// <summary>
    /// 获取渲染管道实例，供流式生成等场景复用。
    /// </summary>
    public ISlideMlRenderPipeline RenderPipeline => _renderPipeline;

    /// <summary>
    /// 获取主线程调度器实例，供流式生成等场景复用。
    /// </summary>
    public IMainThreadDispatcher Dispatcher => _dispatcher;

    /// <summary>
    /// 渲染完成后触发，携带最新的 <see cref="LatestPreviewImage"/>。
    /// </summary>
    public event Action? SlideRendered;

    /// <summary>
    /// 渲染预览图片缓存，供 UI 读取。
    /// </summary>
    public IPreviewImage? LatestPreviewImage { get; private set; }

    /// <summary>
    /// 最近一次渲染的警告列表。
    /// </summary>
    public string LatestWarnings { get; private set; } = string.Empty;

    /// <summary>
    /// 最近一次渲染后回填的 XML。
    /// </summary>
    public string LatestRenderedXml { get; private set; } = string.Empty;

    /// <summary>
    /// 最近一次输入的 SlideML XML。
    /// </summary>
    public string LatestSlideXml { get; private set; } = string.Empty;

    /// <summary>
    /// 将渲染结果应用到当前实例的 Latest* 属性，供流式渲染路径复用。
    /// </summary>
    /// <param name="result">渲染结果。</param>
    public void ApplyRenderResult(SlideMlRenderResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        _ = _dispatcher.InvokeAsync(() =>
        {
            LatestPreviewImage = result.PreviewImage;
            LatestWarnings = result.Warnings.Count == 0
                ? "(none)"
                : string.Join("\n", result.Warnings);
            LatestRenderedXml = result.OutputXml;
            LatestSlideXml = result.InputXml;
            SlideRendered?.Invoke();
            return Task.CompletedTask;
        });
    }

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
        var image = LatestPreviewImage;
        if (image is null)
        {
            return new TextContent("[get_render_preview] 错误：尚未渲染任何页面，请先调用 render_slide。");
        }

        using var memoryStream = new MemoryStream();
        if (Dispatcher.CheckAccess())
        {
          image.Save(memoryStream);
        }
        else
        {
            await Dispatcher.InvokeAsync(() =>
            {
                image.Save(memoryStream);
                return Task.CompletedTask;
            });
        }
        memoryStream.Position = 0;
        return await DataContent.LoadFromAsync(memoryStream, "image/png").ConfigureAwait(false);
    }

    /// <summary>
    /// 获取最近一次渲染的预览图 <see cref="DataContent"/>，失败返回 null。
    /// </summary>
    public async Task<DataContent?> CreatePreviewDataContentAsync(CancellationToken cancellationToken = default)
    {
        var image = LatestPreviewImage;
        if (image is null)
        {
            return null;
        }

        var memoryStream = new MemoryStream();
        image.Save(memoryStream);
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

        SlideMlRenderResult renderResult;
        try
        {
            renderResult = await _renderPipeline.RenderAsync(slideXml, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return $"[render_slide] 渲染异常：{ex.Message}";
        }

        _ = _dispatcher.InvokeAsync(() =>
        {
            LatestPreviewImage = renderResult.PreviewImage;
            LatestWarnings = renderResult.Warnings.Count == 0
                ? "(none)"
                : string.Join("\n", renderResult.Warnings);
            LatestRenderedXml = renderResult.OutputXml;
            LatestSlideXml = renderResult.InputXml;
            SlideRendered?.Invoke();
            return Task.CompletedTask;
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

        return builder.ToString();
    }
}
