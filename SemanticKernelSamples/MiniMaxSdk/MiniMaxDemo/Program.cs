using MiniMaxSdk;
using MiniMaxSdk.Images.Models;

var miniMaxKeyFile = @"C:\lindexi\Work\Key\MiniMax.txt";
var miniMaxKey = File.ReadAllText(miniMaxKeyFile).Trim();

var miniMaxClient = new MiniMaxClient(miniMaxKey);
var imageGeneration = miniMaxClient.ImageGeneration;
var result = await imageGeneration.GenerateAsync(new MiniMaxImageGenerationRequest("一只在月光下坐在书堆上的布偶猫，旁边有发光的萤火虫，电影感，细节丰富，高清插画风格"));
var outputDirectory = Path.Join(AppContext.BaseDirectory, "GeneratedImages");
Directory.CreateDirectory(outputDirectory);

for (var i = 0; i < result.Images.Count; i++)
{
    var image = result.Images[i];
    var outputFile = new FileInfo(Path.Join(outputDirectory, $"minimax-image-{i + 1}{image.SuggestedFileExtension}"));
    await image.SaveAsync(outputFile);
    Console.WriteLine($"图片已输出：{outputFile.FullName}");
}

Console.WriteLine("Hello, World!");
