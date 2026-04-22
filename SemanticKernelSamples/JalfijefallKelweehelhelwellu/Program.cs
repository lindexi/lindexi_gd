using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

using OllamaSharp;
using System.Text;

Console.OutputEncoding = Encoding.UTF8;

var ollamaEndpoint = new Uri("http://172.20.113.28:11434");
const string modelId = "qwen3-vl:8b";
const string imagePath = @"f:\temp\20260422085613.png";

if (!File.Exists(imagePath))
{
    throw new FileNotFoundException($"找不到测试图片：{imagePath}", imagePath);
}

var prompt = """
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

var imageBytes = await File.ReadAllBytesAsync(imagePath);
var mimeType = GetImageMimeType(imagePath);

var ollamaApiClient = new OllamaApiClient(ollamaEndpoint, modelId);
AIAgent agent = new ChatClientAgent
(
    ollamaApiClient,
    instructions:
    """
    You analyze desktop screenshots.
    Reply in Chinese with 1-2 sentences.
    Name the visible application, page, or document whenever it can be identified.
    Describe the current task and include only details that are actually visible.
    If a detail is unclear, say so instead of guessing.
    """
    );

var message = new ChatMessage
{
    Role = ChatRole.User,
    Contents = 
    [
        new TextContent(prompt),
        new DataContent(imageBytes, mimeType)
    ]
};

var agentResponse = await agent.RunAsync(message);

Console.WriteLine("识别结果：");
Console.WriteLine(agentResponse.Text);

return;

static string GetImageMimeType(string imagePath)
{
    ArgumentException.ThrowIfNullOrWhiteSpace(imagePath);

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
