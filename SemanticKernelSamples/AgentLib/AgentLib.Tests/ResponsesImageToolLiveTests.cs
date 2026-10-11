#pragma warning disable OPENAI001

using System.IO.Compression;
using System.Text;
using System.Text.Json;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders;
using AgentLib.Logging;
using AgentLib.Tools;
using Microsoft.Extensions.AI;
using OpenAI.Responses;

namespace AgentLib.Tests;

[TestClass]
[TestCategory("LiveApi")]
[DoNotParallelize]
public sealed class ResponsesImageToolLiveTests
{
    [TestMethod]
    public async Task ImageTool_ReturnsNativeImageAndModelRecognizesItAsync()
    {
        const string keyPath = @"C:\lindexi\Work\Key\MiniMax.txt";
        if (!File.Exists(keyPath)) return;
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        string reportPath = Path.Combine(AppContext.BaseDirectory, $"image-tool-{Guid.NewGuid():N}.json");
        using var recorder = new ImageRequestRecorder();
        using var httpClient = new HttpClient(recorder);
        string key = (await File.ReadAllTextAsync(keyPath, cancellation.Token)).Trim();
        var manager = new CopilotChatManager(new EmptyCopilotChatLogger());
        manager.AgentApiEndpointManager.HttpClient?.Dispose();
        manager.AgentApiEndpointManager.HttpClient = httpClient;
        manager.AgentApiEndpointManager.LoadConfiguration(AgentApiManagerConfiguration.FromJsonString(JsonSerializer.Serialize(new
        {
            PrimaryModel = "MiniMax-M3",
            OpenAIConfigurationList = new[]
            {
                new { EndPoint = "https://api.minimaxi.com/v1", Key = key,
                    ModelDefinitions = new[] { new { Provider = "minimax", ModelName = "MiniMax-M3", ModelId = "MiniMax-M3" } } }
            }
        })));
        var context = await manager.CreateManualSendMessageContextAsync(cancellation.Token);
        var client = await ((IResponsesClientProvider)context.LanguageModel).GetResponsesClientAsync();
        int invocationCount = 0;
        var imageBytes = CreateImage();
        await File.WriteAllBytesAsync(Path.ChangeExtension(reportPath, ".png"), imageBytes, cancellation.Token);
        var function = AIFunctionFactory.Create(() =>
        {
            invocationCount++;
            return new DataContent(imageBytes, "image/png");
        }, "load_sample_image", "Load the sample image for visual inspection.");
        var functions = new Dictionary<string, AIFunction> { [function.Name] = function };
        var request = new CreateResponseOptions
        {
            Model = context.LanguageModel.ModelDefinition.ModelId,
            Instructions = "Call load_sample_image to inspect the image. After receiving it, name the solid background color in English. Do not guess before seeing the image.",
            StoredOutputEnabled = false,
            MaxOutputTokenCount = 2048,
        };
        request.Tools.Add(ResponsesToolHelper.CreateTool(function));
        context.UserChatMessage.AppendText("Load the sample image using the tool and identify its background color.");
        await context.AppendMessagesToSessionAsync();
        request.InputItems.Add(ResponseItem.CreateUserMessageItem(context.UserChatMessage.Content));
        try
        {
            using var run = context.StartChatting();
            var first = (await client.CreateResponseAsync(request, cancellation.Token)).Value;
            context.AssistantChatMessage.ResponseInfo.AppendResponse(first);
            var call = first.OutputItems.OfType<FunctionCallResponseItem>().Single();
            var output = await ResponsesToolHelper.InvokeAsync(functions[call.FunctionName], call, cancellation.Token);
            var continuation = new CreateResponseOptions
            {
                Model = request.Model,
                Instructions = request.Instructions,
                StoredOutputEnabled = false,
                MaxOutputTokenCount = 2048,
            };
            continuation.Tools.Add(ResponsesToolHelper.CreateTool(function));
            foreach (var item in request.InputItems) continuation.InputItems.Add(item);
            foreach (var item in first.OutputItems) continuation.InputItems.Add(item);
            continuation.InputItems.Add(output);

            var second = (await client.CreateResponseAsync(continuation, cancellation.Token)).Value;
            context.AssistantChatMessage.ResponseInfo.AppendResponse(second);

            Assert.AreEqual(1, invocationCount);
            using var sent = JsonDocument.Parse(recorder.Requests[1]);
            var submitted = sent.RootElement.GetProperty("input").EnumerateArray()
                .Single(item => item.GetProperty("type").GetString() == "function_call_output");
            Assert.AreEqual(call.CallId, submitted.GetProperty("call_id").GetString());
            Assert.AreEqual("input_image", submitted.GetProperty("output")[0].GetProperty("type").GetString());
            Assert.IsTrue(second.GetOutputText().Contains("red", StringComparison.OrdinalIgnoreCase), second.GetOutputText());
            Assert.IsNotNull(second.Usage);
        }
        finally
        {
            await File.WriteAllTextAsync(reportPath, JsonSerializer.Serialize(new
            {
                RecordedAt = DateTimeOffset.UtcNow, invocationCount,
                Requests = recorder.Requests, Responses = recorder.Responses,
            }, new JsonSerializerOptions { WriteIndented = true }), CancellationToken.None);
            Console.WriteLine($"Image tool report: {reportPath}");
        }
    }

    // 测试图片直接生成 PNG，不依赖平台绘图库或磁盘素材。
    private static byte[] CreateImage()
    {
        using var png = new MemoryStream();
        png.Write(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
        WriteChunk(png, "IHDR", new byte[] { 0, 0, 0, 128, 0, 0, 0, 128, 8, 2, 0, 0, 0 });
        using var compressed = new MemoryStream();
        using (var zlib = new ZLibStream(compressed, CompressionLevel.Optimal, leaveOpen: true))
        {
            byte[] row = new byte[1 + 128 * 3];
            for (int x = 0; x < 128; x++) row[1 + x * 3] = 255;
            for (int y = 0; y < 128; y++) zlib.Write(row);
        }
        WriteChunk(png, "IDAT", compressed.ToArray());
        WriteChunk(png, "IEND", []);
        return png.ToArray();
    }

    private static void WriteChunk(Stream stream, string type, byte[] data)
    {
        byte[] length = new byte[4];
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(length, data.Length);
        stream.Write(length);
        byte[] body = Encoding.ASCII.GetBytes(type).Concat(data).ToArray();
        stream.Write(body);
        uint crc = uint.MaxValue;
        foreach (byte value in body)
        {
            crc ^= value;
            for (int bit = 0; bit < 8; bit++) crc = (crc >> 1) ^ ((crc & 1) == 0 ? 0u : 0xedb88320u);
        }
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(length, ~crc);
        stream.Write(length);
    }

    private sealed class ImageRequestRecorder() : DelegatingHandler(new HttpClientHandler())
    {
        public List<string> Requests { get; } = [];
        public List<string> Responses { get; } = [];
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request.Content);
            Requests.Add(await request.Content.ReadAsStringAsync(cancellationToken));
            var response = await base.SendAsync(request, cancellationToken);
            string body = await response.Content.ReadAsStringAsync(cancellationToken);
            Responses.Add($"HTTP {(int)response.StatusCode}\n{body}");
            Console.WriteLine(Responses[^1]);
            return response;
        }
    }
}
