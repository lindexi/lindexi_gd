using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using DotNetCampus.ModelContextProtocol.Clients;
using PptxGenerator.Models;

namespace PptxGenerator.Rendering;

internal sealed class McpSlideMlRenderPipeline : ISlideMlRenderPipeline
{
    private readonly McpClient _mcpClient;
    private readonly string _renderToolName;

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

    public async Task<SlideMlRenderResult> RenderAsync(string slideXml, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slideXml))
        {
            throw new ArgumentException("SlideML 不能为空。", nameof(slideXml));
        }

        var jsonObject = new JsonObject { ["slideXml"] = slideXml };
        using var jsonDocument = JsonDocument.Parse(jsonObject.ToJsonString());
        var callToolResult = await _mcpClient.CallToolAsync(_renderToolName, jsonDocument.RootElement, cancellationToken)
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