using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotNetCampus.ModelContextProtocol.Clients;
using DotNetCampus.ModelContextProtocol.Protocol.Messages;
using PptxGenerator.Models;

namespace PptxGenerator.Rendering;

/// <summary>
/// 可切换的 SlideML 渲染管道，优先使用 MCP 远程渲染，未连接时回退到本地渲染。
/// </summary>
public sealed class SwitchableSlideMlRenderPipeline : ISlideMlRenderPipeline
{
    private readonly ISlideMlRenderPipeline _defaultPipeline;
    private McpSlideMlRenderPipeline? _mcpPipeline;

    /// <summary>
    /// 初始化 <see cref="SwitchableSlideMlRenderPipeline"/> 的新实例。
    /// </summary>
    /// <param name="defaultPipeline">默认本地渲染管道。</param>
    public SwitchableSlideMlRenderPipeline(ISlideMlRenderPipeline defaultPipeline)
    {
        ArgumentNullException.ThrowIfNull(defaultPipeline);
        _defaultPipeline = defaultPipeline;
    }

    /// <summary>
    /// 获取当前是否已启用 MCP 渲染管道。
    /// </summary>
    public bool IsMcpEnabled => _mcpPipeline is not null;

    /// <summary>
    /// 尝试连接 MCP 服务并切换到 MCP 渲染管道；失败时回退到本地渲染。
    /// </summary>
    /// <param name="mcpServiceUrl">MCP 服务地址。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>连接并切换成功返回 <see langword="true"/>，否则返回 <see langword="false"/>。</returns>
    public async Task<bool> TryEnableMcpAsync(string? mcpServiceUrl, CancellationToken cancellationToken = default)
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
