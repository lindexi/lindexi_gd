// See https://aka.ms/new-console-template for more information

using YikelnukairjurCelcerlurkeneka;

var phiIntentionRecognition = new PhiIntentionRecognition();
var input = "小象过河的图片";
input = "仿写夜宿山寺";
input = "一年级下半个学期，学生的语文学习学情分析";
var result = await phiIntentionRecognition.RecognizeAsync(input);

Console.WriteLine($"Input={input} Success={result.Success} Intention={result.Intention}");

Console.WriteLine("Hello, World!");