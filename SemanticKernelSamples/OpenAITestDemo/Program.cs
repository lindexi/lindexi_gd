
using OpenAI;

using System.ClientModel;
using Microsoft.Extensions.AI;
using OpenAI.Chat;
using OpenAI.Models;

var openAiClient = new OpenAIClient(new ApiKeyCredential("ollama"/*required but ignored*/), new OpenAIClientOptions()
{
    Endpoint = new Uri("http://172.20.113.28:11434/v1/")
});

try
{
    ClientResult<OpenAIModelCollection> modelCollectionResult =
        await openAiClient.GetOpenAIModelClient().GetModelsAsync();
    foreach (var openAiModel in modelCollectionResult.Value)
    {
        Console.WriteLine(openAiModel.Id);
    }
}
catch (Exception e)
{
    Console.WriteLine(e);
}

var agent = openAiClient.GetChatClient("qwen3.8")
    .AsAIAgent();
await foreach (var agentResponseUpdate in agent.RunStreamingAsync("我看到两个单词： narrows （具体而言是 narrows-gateway） 和 navimaxx-cc 。这是在编程领域的，请问这两个词可能的含义和关系分别是什么"))
{
    foreach (var aiContent in agentResponseUpdate.Contents)
    {
        if (aiContent is TextReasoningContent textReasoningContent)
        {
            Console.Write(textReasoningContent.Text);
        }

        if (aiContent is TextContent textContent)
        {
            Console.Write(textContent.Text);
        }
    }
}

