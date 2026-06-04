using Avalonia.Media.Imaging;
using Avalonia.Threading;

using Microsoft.Extensions.AI;

using System;
using System.ComponentModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PptxGenerator;

/// <summary>
/// 将 <see cref="SlideRenderer.RenderAsync"/> 封装为 AI Tool，
/// 供模型在流式对话中自行调用并基于反馈修正。
/// </summary>
public sealed class SlideRenderTool
{
    private readonly SlideRenderer _slideRenderer;

    public SlideRenderTool(SlideRenderer slideRenderer)
    {
        _slideRenderer = slideRenderer ?? throw new ArgumentNullException(nameof(slideRenderer));
    }

    /// <summary>
    /// 渲染预览 Bitmap 缓存，供 UI 读取。
    /// </summary>
    public Bitmap? LatestPreviewBitmap { get; private set; }

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
            renderResult = await _slideRenderer.RenderAsync(slideXml, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return $"[render_slide] 渲染异常：{ex.Message}";
        }

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            LatestPreviewBitmap = renderResult.PreviewBitmap;
            LatestWarnings = renderResult.Warnings.Count == 0
                ? "(none)"
                : string.Join("\n", renderResult.Warnings);
            LatestRenderedXml = renderResult.OutputXml;
            LatestSlideXml = renderResult.InputXml;
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