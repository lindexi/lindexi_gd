using AgentLib.Core;
using AgentLib.Model;
using AgentLib.Tools;

using Microsoft.Extensions.AI;

namespace AgentLib.Coding.Tests;

[TestClass]
public sealed class CodingImageSubAgentToolTests
{
    [TestMethod(DisplayName = "无图片路径时应返回自然语言错误")]
    public async Task AnalyzeImageAsync_WhenFilePathIsEmpty_ReturnsErrorMessage()
    {
        AIFunction tool = CreateTool();

        object? result = await tool.InvokeAsync(new AIFunctionArguments
        {
            ["filePath"] = Array.Empty<string>(),
            ["analysisInstruction"] = "描述图片",
        });

        StringAssert.Contains(result?.ToString(), "图片文件路径数组不能为空");
    }

    [TestMethod(DisplayName = "分析指令为空时应返回自然语言错误")]
    public async Task AnalyzeImageAsync_WhenAnalysisInstructionIsEmpty_ReturnsErrorMessage()
    {
        AIFunction tool = CreateTool();

        object? result = await tool.InvokeAsync(new AIFunctionArguments
        {
            ["filePath"] = new[] { "image.png" },
            ["analysisInstruction"] = " ",
        });

        StringAssert.Contains(result?.ToString(), "图片分析指令不能为空");
    }

    [TestMethod(DisplayName = "文件不存在时应返回自然语言错误")]
    public async Task AnalyzeImageAsync_WhenFileDoesNotExist_ReturnsErrorMessage()
    {
        AIFunction tool = CreateTool();
        string missingFilePath = Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}.png");

        object? result = await tool.InvokeAsync(new AIFunctionArguments
        {
            ["filePath"] = new[] { missingFilePath },
            ["analysisInstruction"] = "描述图片",
        });

        StringAssert.Contains(result?.ToString(), "无法读取图片");
    }

    [TestMethod(DisplayName = "非图片文件应返回自然语言错误")]
    public async Task AnalyzeImageAsync_WhenFileIsNotImage_ReturnsErrorMessage()
    {
        string filePath = Path.Combine(Path.GetTempPath(), $"image-analysis-{Guid.NewGuid():N}.txt");
        await File.WriteAllTextAsync(filePath, "not an image");
        AIFunction tool = CreateTool();

        object? result = await tool.InvokeAsync(new AIFunctionArguments
        {
            ["filePath"] = new[] { filePath },
            ["analysisInstruction"] = "描述图片",
        });

        StringAssert.Contains(result?.ToString(), "不是受支持的图片");
    }

    private static AIFunction CreateTool()
    {
        var provider = new CodingImageSubAgentTool(new AgentApiEndpointManager());
        CopilotChatMessage assistantMessage = CopilotChatMessage.CreateAssistant(
            CopilotChatMessage.PlaceholderContent,
            false);
        return (AIFunction)provider.CreateRunToolRegistrations(string.Empty, assistantMessage).Single().Tool;
    }
}
