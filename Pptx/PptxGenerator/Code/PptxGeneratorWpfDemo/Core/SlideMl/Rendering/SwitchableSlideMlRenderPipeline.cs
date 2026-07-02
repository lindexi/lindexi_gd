using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using DotNetCampus.ModelContextProtocol.Clients;
using DotNetCampus.ModelContextProtocol.Protocol.Messages;
using PptxGenerator.Models;
using PptxGenerator.Rendering;

namespace PptxGenerator;

/// <summary>
/// 可切换的 SlideML 渲染管道。内部持有一个默认的本地渲染管道和一个可选的 MCP 远程渲染管道。
/// 当 MCP 管道可用时自动切换为 MCP 渲染；不可用时回退到默认本地渲染。
/// </summary>
internal sealed class SwitchableSlideMlRenderPipeline : ISlideMlRenderPipeline
{
    private readonly ISlideMlRenderPipeline _defaultPipeline;
    private McpSlideMlRenderPipeline? _mcpPipeline;

    /// <summary>
    /// 初始化 <see cref="SwitchableSlideMlRenderPipeline"/> 的新实例。
    /// </summary>
    /// <param name="defaultPipeline">默认的本地渲染管道。</param>
    public SwitchableSlideMlRenderPipeline(ISlideMlRenderPipeline defaultPipeline)
    {
        ArgumentNullException.ThrowIfNull(defaultPipeline);
        _defaultPipeline = defaultPipeline;
    }

    /// <summary>
    /// 获取当前是否已切换到 MCP 渲染管道。
    /// </summary>
    public bool IsMcpEnabled => _mcpPipeline is not null;

    /// <summary>
    /// 尝试连接 MCP 服务并切换到 MCP 渲染管道。
    /// 连接失败或未找到渲染工具时保持使用默认本地渲染管道。
    /// </summary>
    /// <param name="mcpServiceUrl">MCP 服务地址。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>是否成功切换到 MCP 渲染管道。</returns>
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
            var builder = new McpClientBuilder("SlideML", "1.0.0");
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

        _mcpPipeline = new McpSlideMlRenderPipeline(mcpClient, renderTool.Name);
        return true;
    }

    /// <inheritdoc />
    public async Task<SlideMlRenderResult> RenderAsync(string slideXml, CancellationToken cancellationToken = default)
    {
        var pipeline = _mcpPipeline ?? _defaultPipeline;
        return await pipeline.RenderAsync(slideXml, cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// 基于 MCP 工具的 SlideML 渲染管道实现。
/// 通过连接外部 MCP 服务器，调用其提供的渲染工具完成 SlideML 渲染。
/// </summary>
internal sealed class McpSlideMlRenderPipeline : ISlideMlRenderPipeline
{
    private readonly McpClient _mcpClient;
    private readonly string _renderToolName;

    /// <summary>
    /// 初始化 <see cref="McpSlideMlRenderPipeline"/> 的新实例。
    /// </summary>
    /// <param name="mcpClient">已连接的 MCP 客户端。</param>
    /// <param name="renderToolName">MCP 服务器上渲染工具的名称。</param>
    public McpSlideMlRenderPipeline(McpClient mcpClient, string renderToolName)
    {
        ArgumentNullException.ThrowIfNull(mcpClient);
        if (string.IsNullOrWhiteSpace(renderToolName))
        {
            throw new ArgumentException("渲染工具名称不能为空。", nameof(renderToolName));
        }

        _mcpClient = mcpClient;
        _renderToolName = renderToolName;
    }

    /// <inheritdoc />
    public async Task<SlideMlRenderResult> RenderAsync(string slideXml, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slideXml))
        {
            throw new ArgumentException("SlideML 不能为空。", nameof(slideXml));
        }

        var jsonObject = new JsonObject { ["slideXml"] = slideXml };
        var jsonString = jsonObject.ToJsonString();
        var jsonElement = JsonElement.Parse(jsonString);

        var callToolResult = await _mcpClient.CallToolAsync(_renderToolName, jsonElement, cancellationToken)
            .ConfigureAwait(false);

        if (callToolResult.IsError is true)
        {
            return new SlideMlRenderResult
            {
                InputXml = slideXml,
                OutputXml = slideXml,
                Errors = ["[MCP] 渲染工具返回错误"],
            };
        }

        var structuredContent = callToolResult.StructuredContent;
        if (structuredContent is null)
        {
            return new SlideMlRenderResult
            {
                InputXml = slideXml,
                OutputXml = slideXml,
                Errors = ["[MCP] 渲染工具未返回结构化结果"],
            };
        }

        var mcpResult = structuredContent.Value.Deserialize<McpSlideMlRenderResult>(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        });

        if (mcpResult is null)
        {
            return new SlideMlRenderResult
            {
                InputXml = slideXml,
                OutputXml = slideXml,
                Errors = ["[MCP] 无法反序列化渲染结果"],
            };
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
}
