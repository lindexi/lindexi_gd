// See https://aka.ms/new-console-template for more information

using System.Net.Http.Json;

var url = "http://172.20.114.91:5017";

var httpClient = new HttpClient();

while (true)
{
    Console.WriteLine($"请输入聊天内容");
    var prompt = Console.ReadLine();
    if (string.IsNullOrEmpty(prompt))
    {
        prompt = """
                 <|system|>
                 "You are an AI assistant that can help the user with a variety of tasks. You have access to the following functions:
                 
                 [
                     {
                         "name": "function_name",
                         "description": "function_description",
                         "parameters": [
                             {
                                 "name": "parameter_name",
                                 "type": "parameter_type",
                                 "description": "parameter_description"
                             },
                             ...
                         ],
                         "required": [ "required_parameter_name_1", ... ],
                         "returns": [
                             {
                                 "name": "output_name",
                                 "type": "output_type",
                                 "description": "output_description"
                             },
                             ...
                         ]
                     },
                     ...
                 ] 
                 
                 When the user asks you a question, if you need to use functions, provide function calls in the format:
                 $$[
                     { "name": "function_name", "params": { dictionary containing parameters}, "output": "The output variable name, to be possibly used as input for another function"}.
                     ...
                 ]$$
                 
                 Here is a list of functions you can use:
                 [
                     {
                         "name": "get_weather_data",
                         "parameters": [
                             {
                                 "name": "city",
                                 "type": "string",
                                 "description": "The name of the city for which weather data is required."
                             }
                         ],
                         "returns": [
                             {
                                 "name": "weather_data",
                                 "type": "dictionary",
                                 "description": "A dictionary containing weather data for the specified city."
                             }
                         ]
                     }
                 ]
                 
                 <|end|>
                 <|user|>
                 天津的天气如何<|end|>
                 <|assistant|>
                 """;

        // 请帮我将以下内容转述为地道的英文：你是一个意图识别机器人。你需要根据用户输入的内容，识别出用户的意图。请只输出以下列表里面的用户意图，不要回答任何问题，不要包含其他内容。如果用户的意图不在列表里面，请返回 Other 
        prompt = """
                 <|system|>
                 You are an entity designed to detect the intent behind user inputs. Based on the user's input, you must identify the user's intent from the provided list. Respond solely with the intent from the list, without asking any further questions or including additional information. If the user's intent is not listed, please respond with "Other".
                 Intent list:
                 - 生成课程计划
                 - 生成单词卡
                 - 总结文本
                 - Other
                 
                 Example:
                 User: 帮忙生成课程计划
                 Assistant: 生成课程计划
                 <|end|>
                 <|user|>
                 请帮我生成课程的教学计划<|end|>
                 <|assistant|>
                 """;
    }
    else
    {
        prompt = $@"<|user|>{prompt}<|end|><|assistant|>";
    }
    var chatRequest = new ChatRequest(prompt);

    var httpRequestMessage = new HttpRequestMessage();
    httpRequestMessage.Content = JsonContent.Create(chatRequest);
    httpRequestMessage.Method = HttpMethod.Post;
    httpRequestMessage.RequestUri = new Uri($"{url}/Chat");

    using var httpResponseMessage = await httpClient.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead);

    using var stream = await httpResponseMessage.Content.ReadAsStreamAsync();
    using var streamReader = new StreamReader(stream);
    var buffer = new char[1];

    while (streamReader.ReadBlock(buffer.AsSpan()) > 0)
    {
        Console.Write(buffer[0]);
    }

    Console.WriteLine();
    Console.WriteLine("聊天完成");
    Console.WriteLine();
}

record ChatRequest(string Prompt);