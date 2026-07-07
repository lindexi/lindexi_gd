using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using DotNetCampus.ModelContextProtocol.Clients;
using DotNetCampus.ModelContextProtocol.Protocol.Messages;
using PptxGenerator.Models;
using PptxGenerator.Rendering;

namespace CoursewarePptxGeneratorWpfDemo.Rendering;

/// <summary>
/// 可切换的课件 SlideML 渲染管道，MCP 可用时使用远程渲染，否则回退本地渲染。
/// </summary>
internal sealed class CoursewareSwitchableSlideMlRenderPipeline : ISlideMlRenderPipeline
{
    private readonly ISlideMlRenderPipeline _defaultPipeline;
    private CoursewareMcpSlideMlRenderPipeline? _mcpPipeline;

    /// <summary>
    /// 初始化 <see cref="CoursewareSwitchableSlideMlRenderPipeline" /> 的新实例。
    /// </summary>
    /// <param name="defaultPipeline">默认本地渲染管道。</param>
    public CoursewareSwitchableSlideMlRenderPipeline(ISlideMlRenderPipeline defaultPipeline)
    {
        ArgumentNullException.ThrowIfNull(defaultPipeline);
        _defaultPipeline = defaultPipeline;
    }

    /// <summary>
    /// 获取当前是否已启用 MCP 渲染管道。
    /// </summary>
    public bool IsMcpEnabled => _mcpPipeline is not null;

    /// <summary>
    /// 尝试连接 MCP 服务并启用远程 SlideML 渲染。
    /// </summary>
    /// <param name="mcpServiceUrl">MCP 服务地址。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>成功启用时返回 true。</returns>
    public async Task<bool> TryEnableMcpAsync(string mcpServiceUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(mcpServiceUrl))
        {
            _mcpPipeline = null;
            return false;
        }

        McpClient mcpClient;
        try
        {
            var builder = new McpClientBuilder("CoursewareSlideML", "1.0.0");
            builder.WithHttp(mcpServiceUrl);
            mcpClient = builder.Build();
        }
        catch
        {
            _mcpPipeline = null;
            return false;
        }

        ListToolsResult toolsResult;
        try
        {
            toolsResult = await mcpClient.ListToolsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            _mcpPipeline = null;
            return false;
        }

        var renderTool = toolsResult.Tools.FirstOrDefault(t =>
            t.Name.Contains("Render", StringComparison.OrdinalIgnoreCase)
            && t.Name.Contains("SlideML", StringComparison.OrdinalIgnoreCase));

        if (renderTool is null)
        {
            _mcpPipeline = null;
            return false;
        }

        _mcpPipeline = new CoursewareMcpSlideMlRenderPipeline(mcpClient, renderTool.Name);
        return true;
    }

    /// <inheritdoc />
    public Task<SlideMlRenderResult> RenderAsync(string slideXml, CancellationToken cancellationToken = default)
    {
        var pipeline = _mcpPipeline ?? _defaultPipeline;
        return pipeline.RenderAsync(slideXml, cancellationToken);
    }
}

/// <summary>
/// 基于 MCP 工具调用的课件 SlideML 渲染管道。
/// </summary>
internal sealed class CoursewareMcpSlideMlRenderPipeline : ISlideMlRenderPipeline
{
    private readonly McpClient _mcpClient;
    private readonly string _renderToolName;

    /// <summary>
    /// 初始化 <see cref="CoursewareMcpSlideMlRenderPipeline" /> 的新实例。
    /// </summary>
    /// <param name="mcpClient">MCP 客户端。</param>
    /// <param name="renderToolName">MCP 渲染工具名称。</param>
    public CoursewareMcpSlideMlRenderPipeline(McpClient mcpClient, string renderToolName)
    {
        ArgumentNullException.ThrowIfNull(mcpClient);
        ArgumentException.ThrowIfNullOrWhiteSpace(renderToolName);

        _mcpClient = mcpClient;
        _renderToolName = renderToolName;
    }

    /// <inheritdoc />
    public async Task<SlideMlRenderResult> RenderAsync(string slideXml, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slideXml);

        var jsonObject = new JsonObject { ["slideXml"] = slideXml };
        using var jsonDocument = JsonDocument.Parse(jsonObject.ToJsonString());
        var callToolResult = await _mcpClient.CallToolAsync(_renderToolName, jsonDocument.RootElement, cancellationToken).ConfigureAwait(false);

        if (callToolResult.IsError is true)
        {
            return CreateErrorResult(slideXml, "[MCP] 渲染工具返回错误");
        }

        var structuredContent = callToolResult.StructuredContent;
        if (structuredContent is null)
        {
            return CreateErrorResult(slideXml, "[MCP] 渲染工具未返回结构化结果");
        }

        var mcpResult = structuredContent.Value.Deserialize<CoursewareMcpSlideMlRenderResult>(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        });

        if (mcpResult is null)
        {
            return CreateErrorResult(slideXml, "[MCP] 无法反序列化渲染结果");
        }

        IPreviewImage? previewImage = null;
        if (!string.IsNullOrWhiteSpace(mcpResult.PreviewImageFilePath) && File.Exists(mcpResult.PreviewImageFilePath))
        {
            previewImage = new FilePreviewImage(new FileInfo(mcpResult.PreviewImageFilePath));
        }

        return new SlideMlRenderResult
        {
            InputXml = slideXml,
            OutputXml = mcpResult.OutputXml,
            Warnings = mcpResult.Warnings,
            Errors = mcpResult.Errors,
            PreviewImage = previewImage,
        };
    }

    private static SlideMlRenderResult CreateErrorResult(string slideXml, string error)
    {
        return new SlideMlRenderResult
        {
            InputXml = slideXml,
            OutputXml = slideXml,
            Errors = [error],
        };
    }
}
