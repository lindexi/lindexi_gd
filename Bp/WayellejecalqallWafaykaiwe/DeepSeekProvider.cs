using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace WayellejecalqallWafaykaiwe;

record ChatRequest(string Prompt);

//public class DeepSeekChatCompletionService : IChatCompletionService
//{
//    public IReadOnlyDictionary<string, object?> Attributes { get; } = new Dictionary<string, object?>
//    {
//        { "name", "DeepSeek" },
//        { "version", "1.0" },
//    };

//    public async Task<IReadOnlyList<ChatMessageContent>> GetChatMessageContentsAsync(ChatHistory chatHistory, PromptExecutionSettings? executionSettings = null,
//        Kernel? kernel = null, CancellationToken cancellationToken = new CancellationToken())
//    {
//        var chatMessageContents = new List<ChatMessageContent>();
//        await foreach (var streamingChatMessageContent in GetStreamingChatMessageContentsAsync(chatHistory, executionSettings, kernel, cancellationToken))
//        {
//            chatMessageContents.Add(new ChatMessageContent(streamingChatMessageContent.Role ?? AuthorRole.Assistant,
//                streamingChatMessageContent.Content));
//        }

//        return chatMessageContents;
//    }

//    public IAsyncEnumerable<StreamingChatMessageContent> GetStreamingChatMessageContentsAsync(ChatHistory chatHistory,
//        PromptExecutionSettings? executionSettings = null, Kernel? kernel = null,
//        CancellationToken cancellationToken = new CancellationToken())
//    {
//        throw new NotImplementedException();
//    }
//}

public class DeepSeekProvider
{
    public static DeepSeekProvider GetDeepSeekProvider() => new DeepSeekProvider();

    public async Task<DeepSeekResponse> ChatAsync(string prompt)
    {
        using var httpClient = new HttpClient();

        try
        {
            var response = await httpClient.SendAsync
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

            if (response.StatusCode == HttpStatusCode.InternalServerError)
            {
                string? errorMessage = null;
                try
                {
                    errorMessage = await response.Content.ReadAsStringAsync();
                }
                catch (Exception e)
                {
                    // 失败就失败
                }

                return new DeepSeekResponse()
                {
                    Success = false,
                    ErrorMessage = errorMessage,
                    ResponseStream = Stream.Null
                };
            }

            var stream = await response.Content.ReadAsStreamAsync();
            return new DeepSeekResponse()
            {
                Success = true,
                ErrorMessage = null,
                ResponseStream = stream,
                DisposableObject = response,
            };
        }
        catch (Exception e)
        {
            return new DeepSeekResponse()
            {
                Success = false,
                ErrorMessage = e.Message,
                ResponseStream = Stream.Null
            };
        }
    }
}