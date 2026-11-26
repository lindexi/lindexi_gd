using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DotNetCampus.ModelContextProtocol.Clients;
using DotNetCampus.ModelContextProtocol.Protocol.Messages;
using Microsoft.Extensions.AI;
using PptxGenerator.Models;

namespace PptxGenerator.Rendering;

/// <summary>
/// 可切换的 SlideML 渲染管道，优先使用 MCP 远程渲染，未连接时回退到本地渲染。
/// </summary>
public sealed class SwitchableSlideMlRenderPipeline : ISlideMlRenderPipeline, ISlideMlToolProvider
{
    private readonly PipelineState _localState;
    private readonly SemaphoreSlim _switchLock = new(1, 1);
    private PipelineState _activeState;

    /// <summary>
    /// 初始化 <see cref="SwitchableSlideMlRenderPipeline"/> 的新实例。
    /// </summary>
    /// <param name="defaultPipeline">默认本地渲染管道。</param>
    public SwitchableSlideMlRenderPipeline(ISlideMlRenderPipeline defaultPipeline)
    {
        ArgumentNullException.ThrowIfNull(defaultPipeline);
        _localState = new PipelineState(defaultPipeline, []);
        _activeState = _localState;
    }

    /// <summary>
    /// 获取当前是否已启用 MCP 渲染管道。
    /// </summary>
    public bool IsMcpEnabled => Volatile.Read(ref _activeState).Pipeline is McpSlideMlRenderPipeline;

    /// <inheritdoc />
    public IReadOnlyList<AIFunction> SlideMlTools => Volatile.Read(ref _activeState).Tools;

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
                Volatile.Write(ref _activeState, _localState);
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
                Volatile.Write(ref _activeState, _localState);
                return false;
            }

            var tools = new List<Tool>();
            try
            {
                string? cursor = null;
                do
                {
                    var page = await mcpClient.ListToolsAsync(cursor, cancellationToken).ConfigureAwait(false);
                    tools.AddRange(page.Tools);
                    cursor = page.NextCursor;
                } while (!string.IsNullOrEmpty(cursor));
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                Volatile.Write(ref _activeState, _localState);
                throw;
            }
            catch
            {
                Volatile.Write(ref _activeState, _localState);
                return false;
            }

            var renderTool = tools.FirstOrDefault(t =>
                t.Name.Contains("Render", StringComparison.OrdinalIgnoreCase)
                && t.Name.Contains("SlideML", StringComparison.OrdinalIgnoreCase));

            var additionalTools = CreateSlideMlTools(tools,
                (name, arguments, token) => mcpClient.CallToolAsync(name, arguments, token));
            var pipeline = renderTool is null
                ? _localState.Pipeline
                : new McpSlideMlRenderPipeline(mcpClient, renderTool.Name);
            Volatile.Write(ref _activeState, new PipelineState(pipeline, additionalTools));
            return renderTool is not null;
        }
        finally
        {
            _switchLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<SlideMlRenderResult> RenderAsync(string slideXml, CancellationToken cancellationToken = default)
    {
        var pipeline = Volatile.Read(ref _activeState).Pipeline;
        return await pipeline.RenderAsync(slideXml, cancellationToken).ConfigureAwait(false);
    }

    internal static IReadOnlyList<AIFunction> CreateSlideMlTools(IEnumerable<Tool> tools,
        Func<string, JsonElement, CancellationToken, Task<CallToolResult>> callToolAsync)
    {
        var discoveredTools = tools.ToArray();
        var resourceTool = discoveredTools.FirstOrDefault(candidate =>
            string.Equals(candidate.Name, "Register_SlideML_Resource", StringComparison.OrdinalIgnoreCase)
            || string.Equals(candidate.Name, "RegisterSlideMLResource", StringComparison.OrdinalIgnoreCase));
        var markdownTool = discoveredTools.FirstOrDefault(candidate =>
            string.Equals(candidate.Name, "export_courseware_markdown", StringComparison.OrdinalIgnoreCase));

        var functions = new List<AIFunction>(2);
        if (resourceTool is not null)
            functions.Add(new McpFunction(resourceTool, callToolAsync));
        if (markdownTool is not null)
            functions.Add(new McpFunction(markdownTool, callToolAsync));
        return functions.AsReadOnly();
    }

    private sealed record PipelineState(ISlideMlRenderPipeline Pipeline, IReadOnlyList<AIFunction> Tools);

    private sealed class McpFunction(Tool tool, Func<string, JsonElement, CancellationToken, Task<CallToolResult>> callToolAsync) : AIFunction
    {
        public override string Name => tool.Name;
        public override string Description => tool.Description ?? string.Empty;
        public override JsonElement JsonSchema => tool.InputSchema;
        public override JsonElement? ReturnJsonSchema => tool.OutputSchema;

        protected override async ValueTask<object?> InvokeCoreAsync(AIFunctionArguments arguments, CancellationToken cancellationToken)
        {
            var result = await callToolAsync(Name, JsonSerializer.SerializeToElement(arguments), cancellationToken)
                .ConfigureAwait(false);
            if (result.IsError != true && result.StructuredContent is { } content)
                return content;

            return new { result.IsError, result.StructuredContent, result.Content };
        }
    }
}
