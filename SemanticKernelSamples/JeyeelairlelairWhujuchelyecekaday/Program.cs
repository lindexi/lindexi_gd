using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Reasoning;
using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;
using System.ComponentModel;
using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

var folder = @"C:\lindexi\Work\古典名著\庄子\内篇\";

var keyFile = @"C:\lindexi\Work\Doubao.txt";
var key = File.ReadAllText(keyFile);

var outputFile = "output.csv";
using var streamWriter = new StreamWriter(outputFile, Encoding.UTF8,new FileStreamOptions()
{
    Access = FileAccess.ReadWrite,
    Share = FileShare.Read,
})
{
};
streamWriter.WriteLine();
var csvWriter = new CsvWriter(streamWriter, new CsvConfiguration(CultureInfo.CurrentCulture)
{

}, leaveOpen: true);

//for (int i = 0; i < 10; i++)
//{
//    csvWriter.WriteField("abc");
//    csvWriter.WriteField("中文");
//    csvWriter.WriteField("""
//                         换行的内容
//                         第二段
//                         有引号“”
//                         英文:""
//                         是否可以支持的格式
//                         """);
//    csvWriter.NextRecord();
//    await csvWriter.FlushAsync();
//}

var openAiClient = new OpenAIClient(new ApiKeyCredential(key), new OpenAIClientOptions()
{
    Endpoint = new Uri("https://ark.cn-beijing.volces.com/api/v3"),
});

var chatClient = openAiClient.GetChatClient("ep-20260306101224-c8mtg");

var agent = chatClient.AsIChatClient()
    .AsBuilder()
    .BuildAIAgent(new ChatClientAgentOptions()
    {
        ChatOptions = new ChatOptions()
        {
            Tools =
            [
                //AIFunctionFactory.Create(OpenApplication),
                //AIFunctionFactory.Create(WriteFileInfo)
            ]
        }
    });

List<ChatMessage> chatMessageList =
[
    new ChatMessage(ChatRole.System, "请你根据我传入的古籍内容，从中选取出适合做小孩子名字的词。要求不能被轻易起花名，起名应该避开神仙的名字和皇帝溢号。请列出所选的名字的词和选择原因，完成所有选择之后，请调用工具将所选的名字进行整理"),
];
// 如果是给女娃的名字则尽量中性，给男娃的则要求大气，

var files = Directory.GetFiles(folder, "*.txt");

for (int i = 0; i < 100; i++)
{
    if (i > files.Length)
    {
        break;
    }

    //var fileName = $"{i:00}.txt";
    //var file = Path.Join(folder, fileName);
    //if (!File.Exists(file))
    //{
    //    continue;
    //}
    var file = files[i];

    var fileSplitReader = new FileSplitReader(file);
    await foreach (var content in fileSplitReader.ReadAsync())
    {
        var chatMessage = new ChatMessage(ChatRole.User,
            $"""
             古籍片段内容如下：
             {content}
             """);
        ChatMessage[] inputList = [.. chatMessageList, chatMessage];

        agent = chatClient.AsIChatClient()
            .AsBuilder()
            .UseFunctionInvocation(configure: client =>
            {
                client.FunctionInvoker = (context, token) =>
                {
                    context.Terminate = true;
                    return context.Function.InvokeAsync(context.Arguments, token);
                };
            })
            .BuildAIAgent(new ChatClientAgentOptions()
            {
                ChatOptions = new ChatOptions()
                {
                    Tools =
                    [
                        AIFunctionFactory.Create(SetName, name: nameof(SetName)),
                        //AIFunctionFactory.Create(WriteFileInfo)
                    ]
                }
            });

        await DoAsync(inputList);

        [Description("设置名称，需要传入所给的名字和其选择原因")]
        void SetName([Description("名称和其选择理由的列表。每项的格式为 名称：理由")] List<string> nameAndReasonList)
        {
            foreach (var nameAndReason in nameAndReasonList)
            {
                //var nameInfo = new NameInfo(nameAndReason, content, file);

                csvWriter.WriteField(nameAndReason);
                csvWriter.WriteField(content);
                csvWriter.WriteField(file);

                //csvWriter.WriteRecord(nameInfo);
                csvWriter.NextRecord();
                csvWriter.Flush();
            }
        }
    }
}

async Task DoAsync(ChatMessage[] inputList)
{
    await foreach (var reasoningAgentResponseUpdate in agent.RunReasoningStreamingAsync(inputList))
    {
        if (reasoningAgentResponseUpdate.IsFirstThinking)
        {
            Console.WriteLine($"思考：");
        }

        if (reasoningAgentResponseUpdate.IsThinkingEnd && reasoningAgentResponseUpdate.IsFirstOutputContent)
        {
            Console.WriteLine();
            Console.WriteLine("----------");
        }

        Console.Write(reasoningAgentResponseUpdate.Reasoning);
        Console.Write(reasoningAgentResponseUpdate.Text);
    }

    Console.WriteLine();
    Console.WriteLine($"结束");
    Console.WriteLine();
    Console.ReadLine();
}

Console.WriteLine("Hello, World!");

record NameInfo(string NameAndReason, string Content, string FilePath);

record FileSplitReader(string FilePath)
{
    public async IAsyncEnumerable<string> ReadAsync()
    {
        await using var fileStream = File.OpenRead(FilePath);

        using var streamReader = new StreamReader(fileStream, Encoding.GetEncoding("gbk"));
        var buffer = new StringBuilder();
        var maxCharCount = 1000;

        while (true)
        {
            var line = await streamReader.ReadLineAsync();
            if (line is null)
            {
                break;
            }

            if (buffer.Length == 0)
            {
                buffer.AppendLine(line);

                if (buffer.Length > maxCharCount)
                {
                    yield return buffer.ToString();
                    buffer.Clear();
                }
            }
            else
            {
                if (buffer.Length + line.Length > maxCharCount)
                {
                    yield return buffer.ToString();
                    buffer.Clear();
                }

                buffer.AppendLine(line);
            }
        }

        if (buffer.Length > 0)
        {
            yield return buffer.ToString();
        }
    }
}