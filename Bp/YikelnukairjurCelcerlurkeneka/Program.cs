// See https://aka.ms/new-console-template for more information

using YikelnukairjurCelcerlurkeneka;

var phiIntentionRecognition = new PhiIntentionRecognition();

var inputList =
    new string[]
    {
        "小象过河的图片",
        "仿写夜宿山寺",
        "一年级下半个学期，学生的语文学习学情分析",
    };

foreach (var input in inputList)
{
    var result = await phiIntentionRecognition.RecognizeAsync(input);

    Console.WriteLine($"Input={input} Success={result.Success} Intention={result.Intention}");
}


Console.WriteLine("Hello, World!");