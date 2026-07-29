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
    private readonly SemaphoreSlim _switchLock = new(1, 1);
    private ISlideMlRenderPipeline _activePipeline;

    /// <summary>
    /// 初始化 <see cref="SwitchableSlideMlRenderPipeline"/> 的新实例。
    /// </summary>
    /// <param name="defaultPipeline">默认本地渲染管道。</param>
    public SwitchableSlideMlRenderPipeline(ISlideMlRenderPipeline defaultPipeline)
    {
        ArgumentNullException.ThrowIfNull(defaultPipeline);
        _defaultPipeline = defaultPipeline;
        _activePipeline = defaultPipeline;
    }

    /// <summary>
    /// 获取当前是否已启用 MCP 渲染管道。
    /// </summary>
    public bool IsMcpEnabled => !ReferenceEquals(
        Volatile.Read(ref _activePipeline),
        _defaultPipeline);

    /// <summary>
    /// 尝试连接 MCP 服务并切换到 MCP 渲染管道；失败时回退到本地渲染。
    /// </summary>
    /// <param name="mcpServiceUrl">MCP 服务地址。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>连接并切换成功返回 <see langword="true"/>，否则返回 <see langword="false"/>。</returns>
    public async Task<bool> TryEnableMcpAsync(string? mcpServiceUrl, CancellationToken cancellationToken = default)
    {
        await _switchLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (string.IsNullOrWhiteSpace(mcpServiceUrl))
            {
                Volatile.Write(ref _activePipeline, _defaultPipeline);
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
                Volatile.Write(ref _activePipeline, _defaultPipeline);
                return false;
            }

            ListToolsResult toolsResult;
            try
            {
                toolsResult = await mcpClient.ListToolsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                Volatile.Write(ref _activePipeline, _defaultPipeline);
                throw;
            }
            catch
            {
                Volatile.Write(ref _activePipeline, _defaultPipeline);
                return false;
            }

            var renderTool = toolsResult.Tools.FirstOrDefault(t =>
                t.Name.Contains("Render", StringComparison.OrdinalIgnoreCase)
                && t.Name.Contains("SlideML", StringComparison.OrdinalIgnoreCase));

            if (renderTool is null)
            {
                Volatile.Write(ref _activePipeline, _defaultPipeline);
                return false;
            }

            Volatile.Write(
                ref _activePipeline,
                new McpSlideMlRenderPipeline(mcpClient, renderTool.Name));
            return true;
        }
        finally
        {
            _switchLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<SlideMlRenderResult> RenderAsync(string slideXml, CancellationToken cancellationToken = default)
    {
        var pipeline = Volatile.Read(ref _activePipeline);
        return await pipeline.RenderAsync(slideXml, cancellationToken).ConfigureAwait(false);
    }
}
