// See https://aka.ms/new-console-template for more information
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Reasoning;
using Microsoft.Extensions.AI;

using OpenAI;
using OpenAI.Chat;

using System;
using System.ClientModel;
using System.ComponentModel;
using OpenAI.Images;
using VolcEngineSdk;

using ImageGenerationOptions = OpenAI.Images.ImageGenerationOptions;

var keyFile = @"C:\lindexi\Work\Doubao.txt";
var key = File.ReadAllText(keyFile);

var openAiClient = new OpenAIClient(new ApiKeyCredential(key), new OpenAIClientOptions()
{
    Endpoint = new Uri("https://ark.cn-beijing.volces.com/api/v3")
});

ImageClient imageClient = openAiClient.GetImageClient("ep-20260120102721-c4pxb");

var result = await imageClient.GenerateImageAsync("星际穿越，黑洞，黑洞里冲出一辆快支离破碎的复古列车，抢视觉冲击力，电影大片，末日既视感，动感，对比色，oc渲染，光线追踪，动态模糊，景深，超现实主义，深蓝，画面通过细腻的丰富的色彩层次塑造主体与场景，质感真实，暗黑风背景的光影效果营造出氛围，整体兼具艺术幻想感，夸张的广角透视效果，耀光，反射，极致的光影，强引力，吞噬", new ImageGenerationOptions()
{

});

var generatedImage = result.Value;
Console.WriteLine(generatedImage.ImageUri);

Console.WriteLine();

Console.Read();
