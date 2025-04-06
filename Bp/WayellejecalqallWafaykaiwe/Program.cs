// See https://aka.ms/new-console-template for more information

using WayellejecalqallWafaykaiwe;

var deepSeekIntentionRecognition = new DeepSeekIntentionRecognition();

var inputList =
    new string[]
    {
        "小象过河的图片",
        "帮我润色选中的文本",
         "从1至10数字中，随机抽取4个数字",
        "五年二班随机抽取一名学生",
        "仿写夜宿山寺",
        "一年级下半个学期，学生的语文学习学情分析",
    };

foreach (var input in inputList)
{
    var result = await deepSeekIntentionRecognition.RecognizeAsync(input);

    Console.WriteLine($"Input={input} Success={result.Success} Intention={result.Intention}");
}


Console.WriteLine("Hello, World!");