var miniMaxKeyFile = @"C:\lindexi\Work\Key\MiniMax.txt";
var miniMaxKey = File.ReadAllText(miniMaxKeyFile).Trim();

var request = new WaikearcijelliKawcanawjur.MiniMaxImageGenerationRequest(
    Prompt: "一只在月光下坐在书堆上的布偶猫，旁边有发光的萤火虫，电影感，细节丰富，高清插画风格",
    Model: WaikearcijelliKawcanawjur.MiniMaxImageGenerationModels.Image01,
    AspectRatio: WaikearcijelliKawcanawjur.MiniMaxImageAspectRatios.Square,
    ResponseFormat: WaikearcijelliKawcanawjur.MiniMaxImageResponseFormats.Base64,
    Count: 1,
    PromptOptimizer: true,
    AigcWatermark: false);

using var client = new WaikearcijelliKawcanawjur.MiniMaxImageGenerationClient(miniMaxKey);
var result = await client.GenerateAsync(request);

if (result.Images.Count == 0)
{
    throw new InvalidOperationException("MiniMax 未返回任何图片。\n");
}

var outputDirectory = Path.Combine(AppContext.BaseDirectory, "GeneratedImages");
Directory.CreateDirectory(outputDirectory);

for (var i = 0; i < result.Images.Count; i++)
{
    var image = result.Images[i];

    if (!image.HasBinaryContent || image.Bytes is null)
    {
        throw new InvalidOperationException("当前示例期望返回 Base64 图片数据。\n");
    }

    var outputFile = Path.Combine(outputDirectory, $"minimax-image-{i + 1}{image.SuggestedFileExtension}");
    await File.WriteAllBytesAsync(outputFile, image.Bytes);
    Console.WriteLine($"图片已输出：{outputFile}");
}
