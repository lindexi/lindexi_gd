// See https://aka.ms/new-console-template for more information

using System.ClientModel;
using OpenAI;

var key = File.ReadAllText(@"c:\lindexi\Work\Key\ModelLindexi.txt");
var url = "https://model.server.lindexi.com/";

var openAiClient = new OpenAIClient(new ApiKeyCredential(key),new OpenAIClientOptions()
{
    Endpoint = new Uri(url)
});

var imageClient = openAiClient.GetImageClient("gpt-image-2");

var prompt = """
             A minimalist flat vector logo for a GitHub organization. A stylized geometric shape combining a rounded hexagon and a subtle gear motif, formed by three overlapping translucent polygons in blue, purple, and teal. The overlapping area creates a glowing gradient, symbolizing cross-platform unity (Windows, Linux, macOS). Clean lines, no text, white background. Modern tech company style, flat design, vector art, high quality, suitable for app icon.
             """;

var clientResult = await imageClient.GenerateImageAsync(prompt);
var generatedImage = clientResult.Value;
using var imageStream = generatedImage.ImageBytes.ToStream();
var file = Path.Join(AppContext.BaseDirectory, "1.png");
using var fileStream = File.Create(file);
await imageStream.CopyToAsync(fileStream);

Console.WriteLine("Hello, World!");
