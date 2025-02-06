// See https://aka.ms/new-console-template for more information

using System.IO;
using System.Net.Http.Json;
using System.Reflection;

var httpClient = new HttpClient();
var response = await httpClient.PostAsync("http://172.20.114.91:11434/api/generate", JsonContent.Create(new
{
    model = "deepseek-r1:7B",
    prompt = "请你告诉我你知道的天气有哪些？用json格式输出",
    stream = false
}));
var text = await response.Content.ReadAsStringAsync();
Console.WriteLine("Hello, World!");
