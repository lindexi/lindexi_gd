using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Planners;

using System.Numerics;
using Microsoft.SemanticKernel.AI;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI;
using Microsoft.SemanticKernel.Planning;
using Microsoft.SemanticKernel.Reliability.Basic;
using Microsoft.SemanticKernel.TemplateEngine;

var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Debug);
});

// 这里的演示代码需要用到 AzureAI 的支持，需要提前申请好，申请地址：https://aka.ms/oai/access

var endpoint = "https://lindexi.openai.azure.com/"; // 请换成你的地址
var apiKey = Environment.GetEnvironmentVariable("AzureAIKey"); // 请换成你的密钥

if (string.IsNullOrEmpty(apiKey))
{
    throw new ArgumentException($"请设置为你的密钥");
}

IKernel kernel = new KernelBuilder()
    .WithLoggerFactory(loggerFactory)
    .WithAzureChatCompletionService("GPT4", endpoint, apiKey)
    // 当然，这里也可以支持 OpenAI 的服务。或者是其他第三方的服务
    //.WithOpenAIChatCompletionService()
    .WithRetryBasic(new BasicRetryConfig()
    {
        MaxRetryCount = 1000,
        MinRetryDelay = TimeSpan.FromSeconds(1),
    })
    .Build();
kernel.RegisterSemanticFunction("WriterPlugin", "ShortPoem", new PromptTemplateConfig()
{
    Description = "Turn a scenario into a short and entertaining poem.",
    Input = new PromptTemplateConfig.InputConfig()
    {
        Parameters = new List<PromptTemplateConfig.InputParameter>()
      {
          new PromptTemplateConfig.InputParameter()
          {
              Name = "input",
              Description = "The scenario to turn into a poem.",
          }
      }
    }
}, new PromptTemplate(
    @"Generate a short funny poem or limerick to explain the given event. Be creative and be funny. Let your imagination run wild.
Event:{{$input}}
", new PromptTemplateConfig()
    {
    }, kernel));

kernel.CreateSemanticFunction(@"Translate the input below into {{$language}}

MAKE SURE YOU ONLY USE {{$language}}.

{{$input}}

Translation:
",new PromptTemplateConfig()
{
    Input = new PromptTemplateConfig.InputConfig()
    {
        Parameters = new List<PromptTemplateConfig.InputParameter>()
        {
            new PromptTemplateConfig.InputParameter()
            {
                Name = "input",
            },
            new PromptTemplateConfig.InputParameter()
            {
                Name = "language",
                Description = "The language which will translate to",
            }
        }
    },
    Description = "Translate the input into a language of your choice",
    
}, functionName: "Translate", pluginName: "WriterPlugin");

var planner = new SequentialPlanner(kernel, new SequentialPlannerConfig()
{
});

var plan = await planner.CreatePlanAsync("Write a poem about John Doe, then translate it into Chinese.");

var list = plan.Steps;

var planString = plan.ToPlanString();

var result = await kernel.RunAsync(plan);

Console.Read();