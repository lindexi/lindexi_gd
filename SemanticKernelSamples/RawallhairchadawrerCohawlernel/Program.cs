// See https://aka.ms/new-console-template for more information

using System.ClientModel;
using OpenAI;
using OpenAI.Images;
using OpenAI.Models;

var keyFile = @"C:\lindexi\Work\Key\ModelLindexi.txt";
var key = await File.ReadAllTextAsync(keyFile);

var openAiClient = new OpenAIClient(new ApiKeyCredential(key), new OpenAIClientOptions()
{
    Endpoint = new Uri("https://model.server.lindexi.com/v1/")
});

//ClientResult<OpenAIModelCollection> modelsResult = await openAiClient.GetOpenAIModelClient().GetModelsAsync();

//foreach (var model in modelsResult.Value)
//{
//    Console.WriteLine(model.Id);
//}
/*
   gpt-image-2
   gpt-5.3-codex-spark
   gpt-5.4
   gpt-5.4-mini
   gpt-5.6-terra
   gpt-5.6-luna
   codex-auto-review
   gpt-image-1.5
   gpt-5.5
   gpt-5.6-sol
*/
var imageClient = openAiClient.GetImageClient("gpt-image-2");
ClientResult<GeneratedImage> generateImageResult = await imageClient.GenerateImageAsync("帮我生成一个图标，图标里面有猫的元素");

using var stream = generateImageResult.Value.ImageBytes.ToStream();
var outputFile = "output.png";
using var fileStream = File.Create(outputFile);
await stream.CopyToAsync(fileStream);

Console.WriteLine("Hello, World!");


fileStream.Seek(0, SeekOrigin.Begin);
ClientResult<GeneratedImage> editImageResult = await imageClient.GenerateImageEditAsync(fileStream,"output.png","图片采用浅色风格", new ImageEditOptions()
{
    
});