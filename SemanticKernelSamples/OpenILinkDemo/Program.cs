using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

using OpenAI;

using OpenILink.SDK;

using System.ClientModel;
using System.Diagnostics;
using System.Net.Sockets;

var keyFile = @"C:\lindexi\Work\Doubao.txt";
var key = File.ReadAllText(keyFile);

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

var tokenFilePath = Path.Join(AppContext.BaseDirectory, "Token.txt");
string? initialToken = null;
// 初次扫码之前，这个 Token 肯定是空的
if (File.Exists(tokenFilePath))
{
    initialToken = File.ReadAllText(tokenFilePath);
}

// 存放用于读取哪条信息的 Buffer 的内容
var bufferPath = Path.Join(AppContext.BaseDirectory, "GetUpdatesBuffer.txt");
string? initBuffer = null;
if (File.Exists(bufferPath))
{
    initBuffer = File.ReadAllText(bufferPath);
}

var client = OpenILinkClient.Create(initialToken);

if (string.IsNullOrWhiteSpace(client.Token))
{
    var login = await client.LoginWithQrAsync(ShowQrCode, OnScanned);
    if (!login.Connected)
    {
        Console.Error.WriteLine($"登录失败: {login.Message}");
        return;
    }

    Console.WriteLine($"Token={login.BotToken}");
}

File.WriteAllText(tokenFilePath, client.Token);

await client.MonitorAsync(HandleMessageAsync, new MonitorOptions
{
    InitialBuffer = initBuffer,
    OnBufferUpdated = SaveBuffer,
    OnError = ReportError,
    OnSessionExpired = ReportSessionExpired
});

Console.WriteLine("Hello, World!");


void ShowQrCode(string qrCodeImageDownloadUrl)
{
    Process.Start(new ProcessStartInfo(qrCodeImageDownloadUrl)
    {
        UseShellExecute = true
    });
}

void OnScanned()
{
    Console.WriteLine("已扫码，请在微信端确认。");
}

async Task HandleMessageAsync(WeixinMessage message)
{
    while (true)
    {
        try
        {
            await HandleMessageAsyncInner(message);
            return;
        }
        catch (Exception e)
        {
            if (IsCanRetrySocketException(e))
            {
                continue;
            }
            else
            {
                Console.WriteLine($"[HandleMessageAsync] {e}");
            }
        }
    }

}

async Task HandleMessageAsyncInner(WeixinMessage message)
{
    var getConfigResponse = await client.GetConfigAsync(message.FromUserId, message.ContextToken!);

    var text = message.ExtractText();
    if (string.IsNullOrWhiteSpace(text))
    {
        foreach (var messageItem in message.ItemList)
        {
            if (messageItem.Type == MessageItemType.Image)
            {
                var imageItem = messageItem.ImageItem;
                if (imageItem?.Media is { EncryptQueryParam : not null, AesKey :not null} media)
                {
                   var imageData = await client.DownloadFileAsync(media.EncryptQueryParam, media.AesKey);
                   var imageFolder = Path.Join(AppContext.BaseDirectory, "Image");
                   Directory.CreateDirectory(imageFolder);
                   var imageFile = Path.Join(imageFolder, $"{Path.GetRandomFileName()}.png");
                   await File.WriteAllBytesAsync(imageFile,imageData);
                   Process.Start(new ProcessStartInfo(imageFile)
                   {
                       UseShellExecute = true
                   });
                }
            }
        }

        return;
    }

    await client.SendTypingAsync(message.FromUserId, getConfigResponse.TypingTicket!, TypingStatus.Typing);

    Console.WriteLine($"[{message.FromUserId}] {text}");

    var responseText = await GetResponseTextAsync(agent, text);

    Console.WriteLine($"[Bot] {responseText}");

    await client.ReplyTextAsync(message, responseText);
}

void SaveBuffer(string buffer)
{
    File.WriteAllText(bufferPath, buffer);
}

void ReportError(Exception exception)
{
    Console.Error.WriteLine(exception.Message);
}

void ReportSessionExpired()
{
    Console.Error.WriteLine("会话过期，请重新登录。");
}

static bool IsCanRetrySocketException(Exception exception)
{
    if (exception is SocketException socketException)
    {
        if (socketException.SocketErrorCode == SocketError.ConnectionRefused)
        {
            return false;
        }

        return true;
    }
    else if (exception is AggregateException aggregateException)
    {
        foreach (var innerException in aggregateException.InnerExceptions)
        {
            if (IsCanRetrySocketException(innerException))
            {
                return true;
            }
        }
    }
    else
    {
        if (exception.InnerException is { } innerException)
        {
            return IsCanRetrySocketException(innerException);
        }
    }
    return false;
}

async Task<string> GetResponseTextAsync(ChatClientAgent chatClientAgent, string userText)
{
    while (true)
    {
        try
        {
            var responseText = await GetResponseTextAsyncInner(chatClientAgent, userText);
            return responseText;
        }
        catch (Exception e)
        {
            if (IsCanRetrySocketException(e))
            {
                continue;
            }
            else
            {
                throw;
            }
        }
    }
}

async Task<string> GetResponseTextAsyncInner(ChatClientAgent chatClientAgent, string userText)
{
    var agentResponse = await chatClientAgent.RunAsync
    (
        [
            new ChatMessage(ChatRole.System,"你是一个充满积极向上情绪的聊天机器人"),
            new ChatMessage(ChatRole.User, userText)
        ]
    );

    var reason = string.Empty; // 为什么直接用 string 类型？因为预期只有一项

    foreach (ChatMessage agentResponseMessage in agentResponse.Messages)
    {
        foreach (var textReasoningContent in agentResponseMessage.Contents.OfType<TextReasoningContent>())
        {
            reason += textReasoningContent.Text;
        }
    }

    var responseText1 = $"";
    if (!string.IsNullOrEmpty(reason))
    {
        responseText1 =
            $"""
             思考：
             {reason.Trim()}
             -----------
             {agentResponse.Text}
             """;
    }

    if (agentResponse.Usage is { } usage)
    {
        var usageText = $"本次对话总Token消耗：{usage.TotalTokenCount};输入Token消耗：{usage.InputTokenCount};输出Token消耗：{usage.OutputTokenCount},其中思考占{usage.ReasoningTokenCount ?? 0}";
        responseText1 += $"\r\n-----------\r\n{usageText}";
    }

    return responseText1;
}