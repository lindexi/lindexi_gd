// See https://aka.ms/new-console-template for more information

using System.Text.Json;

using VolcEngineSdk;

var keyFile = @"C:\lindexi\Work\Doubao.txt";
var key = File.ReadAllText(keyFile);

var arkClient = new ArkClient(key);

var createTaskResult = await arkClient.ContentGeneration.Tasks.Create("ep-20260119192612-ndzvr", [
    new ArkTextContent("三年级数学分数应用题讲解视频，卡通童趣风格，搭配公园游览路线示意图。内容包含：1. 展示题目：公园游览路线里，2/7是上坡，1/7是下坡，计算平地路线长度占总长度的比例；2. 分析常见错误原因：①错误用上坡分数减下坡分数求平地比例；②未将总路线视为单位1，不懂用整体1减去已占比例；3. 演示正确解法：步骤1，把总路线长度看作单位1（即7/7）；步骤2，计算上坡+下坡的总占比：2/7 + 1/7 = 3/7；步骤3，用1减去上坡下坡总和：1 - 3/7 = 4/7；4. 总结解题要点：分数应用题要明确单位1，同分母分数相加减分母不变、分子相加减。"),
    //new ArkImageContent("https://ark-project.tos-cn-beijing.volces.com/doc_image/seepro_i2v.png")
]);

//var createTaskResult = new ArkCreateTaskResult("cgt-20260119200659-5pvfj");
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