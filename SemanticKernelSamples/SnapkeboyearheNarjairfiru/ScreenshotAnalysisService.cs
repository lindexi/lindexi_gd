using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

using OllamaSharp;

using System.IO;

namespace SnapkeboyearheNarjairfiru;

internal sealed class ScreenshotAnalysisService
{
    private const string Prompt = """
                                  Inspect this screenshot and report the visible work in 1-2 sentences.
                                  State which application, webpage, or document is on screen, what action is being performed, and any trustworthy details such as file names, channels, tabs, or subjects.
                                  Keep the description concrete, concise, and strictly based on what can actually be seen.

                                  GOOD EXAMPLES:
                                  ✓ "Visual Studio is open on `Program.cs`, building an Ollama image-analysis sample with Semantic Kernel packages."
                                  ✓ "A Gmail draft is open for a client update about the release schedule, with the compose window in focus."
                                  ✓ "A Slack thread in `#engineering` is discussing API throttling and retry behavior."

                                  BAD EXAMPLES:
                                  ✗ "Someone is working" (too generic)
                                  ✗ "A website is open" (does not identify the page)
                                  ✗ "The user is doing computer stuff" (not factual enough)
                                  """;

    private readonly ChatClientAgent _agent;

    public ScreenshotAnalysisService(Uri ollamaEndpoint, string modelId)
    {
        ArgumentNullException.ThrowIfNull(ollamaEndpoint);
        ArgumentException.ThrowIfNullOrWhiteSpace(modelId);

        var ollamaApiClient = new OllamaApiClient(ollamaEndpoint, modelId);
        _agent = new ChatClientAgent(
            ollamaApiClient,
            instructions:
            """
            You analyze desktop screenshots.
            Reply in Chinese with 1-2 sentences.
            Name the visible application, page, or document whenever it can be identified.
            Describe the current task and include only details that are actually visible.
            If a detail is unclear, say so instead of guessing.
            """);
    }

    public async Task<string> AnalyzeAsync(string imagePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(imagePath);

        var imageBytes = await File.ReadAllBytesAsync(imagePath, cancellationToken);
        var message = new ChatMessage
        {
            Role = ChatRole.User,
            Contents =
            [
                new TextContent(Prompt),
                new DataContent(imageBytes, GetImageMimeType(imagePath))
            ]
        };

        var response = await _agent.RunAsync(message);
        return string.IsNullOrWhiteSpace(response.Text)
            ? "模型没有返回可用的解读内容。"
            : response.Text.Trim();
    }

    private static string GetImageMimeType(string imagePath)
    {
        return Path.GetExtension(imagePath).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".webp" => "image/webp",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            _ => "application/octet-stream"
        };
    }
}