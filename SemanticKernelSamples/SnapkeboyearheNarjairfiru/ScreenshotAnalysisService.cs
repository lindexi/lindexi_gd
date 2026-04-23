using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

using OllamaSharp;

using System.Globalization;
using System.IO;
using System.Text;

namespace SnapkeboyearheNarjairfiru;

internal sealed class ScreenshotAnalysisService
{
    private const string PromptPrefix = """
                                        Inspect the current screenshot and report the visible work in 1-2 sentences.
                                        You will also receive a timeline of recent screenshot analyses with timestamps.
                                        Use that timeline only as supporting context for recent activity continuity.
                                        The final answer must still describe the current screenshot only.
                                        If the timeline conflicts with the current screenshot, trust the current screenshot.
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

    public async Task<string> AnalyzeAsync(
        string imagePath,
        DateTimeOffset capturedAt,
        IReadOnlyCollection<SnapshotAnalysisContext> recentContexts,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(imagePath);
        ArgumentNullException.ThrowIfNull(recentContexts);

        var imageBytes = await File.ReadAllBytesAsync(imagePath, cancellationToken);
        var message = new ChatMessage
        {
            Role = ChatRole.User,
            Contents =
            [
                new TextContent(BuildPrompt(capturedAt, recentContexts)),
                new DataContent(imageBytes, GetImageMimeType(imagePath))
            ]
        };

        try
        {
            var response = await _agent.RunAsync(message);
            if (!string.IsNullOrEmpty(response.Text))
            {
                return response.Text.Trim();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        return "模型没有返回可用的解读内容。";
    }

    private static string BuildPrompt(DateTimeOffset capturedAt, IReadOnlyCollection<SnapshotAnalysisContext> recentContexts)
    {
        StringBuilder stringBuilder = new();
        stringBuilder.AppendLine(PromptPrefix);
        stringBuilder.AppendLine();
        stringBuilder.AppendLine($"Current screenshot time: {capturedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture)}");
        stringBuilder.AppendLine("Recent screenshot timeline (oldest to newest, up to 10 entries):");

        if (recentContexts.Count == 0)
        {
            stringBuilder.AppendLine("- No prior screenshot analysis is available.");
        }
        else
        {
            foreach (var context in recentContexts.OrderBy(context => context.CapturedAt))
            {
                stringBuilder.Append("- ");
                stringBuilder.Append(context.CapturedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture));
                stringBuilder.Append(" | ");
                stringBuilder.Append(context.DisplayName);
                stringBuilder.Append(" | ");
                stringBuilder.AppendLine(context.AnalysisText.ReplaceLineEndings(" "));
            }
        }

        return stringBuilder.ToString();
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