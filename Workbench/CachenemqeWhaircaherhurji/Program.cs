// See https://aka.ms/new-console-template for more information

using System.Net.Http.Json;
using System.Text.Json.Nodes;

var httpClient = new HttpClient();

var prompt = "请你告诉我你知道的天气有哪些？用json格式输出";
Console.WriteLine(prompt);

while (true)
{
    using var response = await httpClient.SendAsync
    (
        new HttpRequestMessage(HttpMethod.Post, "http://172.20.114.91:11434/api/generate")
        {
            Content = JsonContent.Create(new
            {
                model = "deepseek-r1:7B",
                prompt = prompt,
                stream = true
            })
        }, HttpCompletionOption.ResponseHeadersRead
    );

    await using var stream = await response.Content.ReadAsStreamAsync();
    var streamReader = new StreamReader(stream);

    while (true)
    {
        var line = await streamReader.ReadLineAsync();
        if (line == null)
        {
            break;
        }

        // {"model":"deepseek-r1:7B","created_at":"2025-02-06T06:32:27.5038788Z","response":"\u003cthink\u003e","done":false}
        if (JsonNode.Parse(line) is JsonObject jsonObject)
        {
            var responseValue = jsonObject["response"]?.ToString();
            if (responseValue != null)
            {
                Console.Write(responseValue);
            }
        }
    }

    Console.WriteLine();
    Console.WriteLine($"请输出聊天内容： ");
    prompt = Console.ReadLine();
    if (string.IsNullOrEmpty(prompt))
    {
        break;
    }
}

Console.WriteLine("Hello, World!");
