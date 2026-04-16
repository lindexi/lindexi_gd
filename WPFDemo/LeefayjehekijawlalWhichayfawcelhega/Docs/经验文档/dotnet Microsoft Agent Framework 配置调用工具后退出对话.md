# dotnet Microsoft Agent Framework 配置调用工具后退出对话

有些时候将 Agent 当成一个工具分发源，期望 Agent 在完成调用工具之后就退出对话，不需要将工具结果再次返回给到模型

<!--more-->
<!-- CreateTime:2026/03/26 07:11:29 -->

<!-- 发布 -->
<!-- 博客 -->

本文采用的 Microsoft Agent Framework 为 1.0.0-rc4 版本

现在很多时候都采用模型编排的方式，支持 Agent As Tool 的模式，即将模型当成工具给到另一个模型使用的方式

此时的上级模型可能只是一个调度分发的功能，工具执行的结果不期望返回给到上级模型，希望调用完工具就自动退出对话

在 Microsoft Agent Framework 的函数工具调用是通过 Microsoft.Extensions.AI 的 FunctionInvokingChatClient 提供的

可以通过 UseFunctionInvocation 扩展方法进行配置，配置时可设置 FunctionInvokingChatClient 的 FunctionInvoker 委托属性。通过此 FunctionInvoker 委托属性，可以在每次函数工具被调用的时候触发。在此委托里的 FunctionInvocationContext 参数里面，通过设置 Terminate 属性，即可控制是否在调用完成工具之后立刻退出对话

示例内容如下：

```csharp
OpenAI.Chat.ChatClient chatClient = ...;

var agent = chatClient.AsIChatClient()
    .AsBuilder()
    // 在 ChatClientExtensions.cs 的 WithDefaultAgentMiddleware 会判断是否有 FunctionInvokingChatClient 从而来决定是否注册
    //.Use((innerClient, services) =>
    //{
    //    var functionInvokingChatClient = innerClient.GetService<FunctionInvokingChatClient>();
    //    var invokingChatClient = services.GetService<FunctionInvokingChatClient>();

    //    return innerClient;
    //})
    .UseFunctionInvocation(configure: functionInvokingChatClient =>
    {
        functionInvokingChatClient.FunctionInvoker = (context, token) =>
        {
            // 写入属性，即可在调用函数之后退出
            context.Terminate = true;
            return context.Function.InvokeAsync(context.Arguments, token);
        };
    })
    .BuildAIAgent(options: new ChatClientAgentOptions()
    {
        ChatOptions = new ChatOptions()
        {
            Tools =
            [
                AIFunctionFactory.Create(GetWeather)
            ]
        }
    });

[Description("Get the weather for a given location.")]
static string GetWeather([Description("The location to get the weather for.")] string location,
    [Description("查询天气的日期")] string date)
{
    Console.WriteLine($"调用函数");
    return $"查询不到 {location} 城市信息";
}
```

此时的代码里面，无论调用的函数是什么，都会设置 Terminate 属性，这就意味着一旦模型尝试调用工具，则会在工具完成之后立刻退出对话

```csharp
await foreach (var reasoningAgentResponseUpdate in agent.RunReasoningStreamingAsync(new ChatMessage(ChatRole.User,
                   "今天北京的天气咋样.")))
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

Console.WriteLine($"结束");

Console.WriteLine();

Console.Read();
```

尝试运行代码，可以看到模型在对话过程中，尝试调用 GetWeather 方法。方法执行完成之后，对话也就结束了

如此可见工具的结果不会返回到模型里，如此也就可以很方便地被设计为让模型只负责工具的调用分发，而不让模型对工具返回结果感兴趣

本文代码放在 [github](https://github.com/lindexi/lindexi_gd/tree/743fb8a7885af20c265dbd323dd703c912e41774/SemanticKernelSamples/BejalljeawakihereKearyojileelai) 和 [gitee](https://gitee.com/lindexi/lindexi_gd/blob/743fb8a7885af20c265dbd323dd703c912e41774/SemanticKernelSamples/BejalljeawakihereKearyojileelai) 上，可以使用如下命令行拉取代码。我整个代码仓库比较庞大，使用以下命令行可以进行部分拉取，拉取速度比较快

先创建一个空文件夹，接着使用命令行 cd 命令进入此空文件夹，在命令行里面输入以下代码，即可获取到本文的代码

```
git init
git remote add origin https://gitee.com/lindexi/lindexi_gd.git
git pull origin 743fb8a7885af20c265dbd323dd703c912e41774
```

以上使用的是国内的 gitee 的源，如果 gitee 不能访问，请替换为 github 的源。请在命令行继续输入以下代码，将 gitee 源换成 github 源进行拉取代码。如果依然拉取不到代码，可以发邮件向我要代码

```
git remote remove origin
git remote add origin https://github.com/lindexi/lindexi_gd.git
git pull origin 743fb8a7885af20c265dbd323dd703c912e41774
```

获取代码之后，进入 SemanticKernelSamples/BejalljeawakihereKearyojileelai 文件夹，即可获取到源代码

更多技术博客，请参阅 [博客导航](https://blog.lindexi.com/post/%E5%8D%9A%E5%AE%A2%E5%AF%BC%E8%88%AA.html )