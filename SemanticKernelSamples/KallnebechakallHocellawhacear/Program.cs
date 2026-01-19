// See https://aka.ms/new-console-template for more information

using System.Text.Json;

using VolcEngineSdk;

var keyFile = @"C:\lindexi\Work\Doubao.txt";
var key = File.ReadAllText(keyFile);

var arkClient = new ArkClient(key);

//var createTaskResult = await arkClient.ContentGeneration.Tasks.Create("ep-20260119192612-ndzvr", [
//    new ArkTextContent("无人机以极快速度穿越复杂障碍或自然奇观，带来沉浸式飞行体验  --duration 5 --camerafixed false --watermark true"),
//    new ArkImageContent("https://ark-project.tos-cn-beijing.volces.com/doc_image/seepro_i2v.png")
//]);

var createTaskResult = new ArkCreateTaskResult("cgt-20260119200659-5pvfj");
if (createTaskResult.TaskId is {} taskId)
{
    while (true)
    {
        ArkGetTaskResult arkGetTaskResult = await arkClient.ContentGeneration.Tasks.Get(taskId);
        Console.WriteLine($"状态: {arkGetTaskResult.Status}");
        var videoUrl = arkGetTaskResult.Content?.VideoUrl;
        if (!string.IsNullOrEmpty(videoUrl))
        {
            Console.WriteLine($"视频下载地址： {videoUrl}");
            break;
        }
    }
}

Console.Read();