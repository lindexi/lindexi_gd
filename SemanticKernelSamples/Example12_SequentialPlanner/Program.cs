using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Planners;
using System.Numerics;
using Microsoft.SemanticKernel.AI;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI;
using Microsoft.SemanticKernel.Planning;
using Microsoft.SemanticKernel.Reliability.Basic;
using Microsoft.SemanticKernel.TemplateEngine;
using Microsoft.SemanticKernel.Orchestration;
using System.Xml;

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
}, new PromptTemplate(
    @"Generate a short funny poem or limerick to explain the given event. Be creative and be funny. Let your imagination run wild.
Event:{{$input}}
", new PromptTemplateConfig()
    {
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
    }, kernel));

kernel.CreateSemanticFunction(@"Translate the input below into {{$language}}

MAKE SURE YOU ONLY USE {{$language}}.

{{$input}}

Translation:
", new PromptTemplateConfig()
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

var semanticFunction = kernel.CreateSemanticFunction(
    @"Create an XML plan step by step, to satisfy the goal given, with the available functions.

[AVAILABLE FUNCTIONS]

{{$available_functions}}

[END AVAILABLE FUNCTIONS]

To create a plan, follow these steps:
0. The plan should be as short as possible.
1. From a <goal> create a <plan> as a series of <functions>.
2. A plan has 'INPUT' available in context variables by default.
3. Before using any function in a plan, check that it is present in the [AVAILABLE FUNCTIONS] list. If it is not, do not use it.
4. Only use functions that are required for the given goal.
5. Append an ""END"" XML comment at the end of the plan after the final closing </plan> tag.
6. Always output valid XML that can be parsed by an XML parser.
7. If a plan cannot be created with the [AVAILABLE FUNCTIONS], return <plan />.

All plans take the form of:
<plan>
    <!-- ... reason for taking step ... -->
    <function.{FullyQualifiedFunctionName} ... />
    <!-- ... reason for taking step ... -->
    <function.{FullyQualifiedFunctionName} ... />
    <!-- ... reason for taking step ... -->
    <function.{FullyQualifiedFunctionName} ... />
    (... etc ...)
</plan>
<!-- END -->

To call a function, follow these steps:
1. A function has one or more named parameters and a single 'output' which are all strings. Parameter values should be xml escaped.
2. To save an 'output' from a <function>, to pass into a future <function>, use <function.{FullyQualifiedFunctionName} ... setContextVariable=""<UNIQUE_VARIABLE_KEY>""/>
3. To save an 'output' from a <function>, to return as part of a plan result, use <function.{FullyQualifiedFunctionName} ... appendToResult=""RESULT__<UNIQUE_RESULT_KEY>""/>
4. Use a '$' to reference a context variable in a parameter, e.g. when `INPUT='world'` the parameter 'Hello $INPUT' will evaluate to `Hello world`.
5. Functions do not have access to the context variables of other functions. Do not attempt to use context variables as arrays or objects. Instead, use available functions to extract specific elements or properties from context variables.

DO NOT DO THIS, THE PARAMETER VALUE IS NOT XML ESCAPED:
<function.Name4 input=""$SOME_PREVIOUS_OUTPUT"" parameter_name=""some value with a <!-- 'comment' in it-->""/>

DO NOT DO THIS, THE PARAMETER VALUE IS ATTEMPTING TO USE A CONTEXT VARIABLE AS AN ARRAY/OBJECT:
<function.CallFunction input=""$OTHER_OUTPUT[1]""/>

Here is a valid example of how to call a function ""_Function_.Name"" with a single input and save its output:
<function._Function_.Name input=""this is my input"" setContextVariable=""SOME_KEY""/>

Here is a valid example of how to call a function ""FunctionName2"" with a single input and return its output as part of the plan result:
<function.FunctionName2 input=""Hello $INPUT"" appendToResult=""RESULT__FINAL_ANSWER""/>

Here is a valid example of how to call a function ""Name3"" with multiple inputs:
<function.Name3 input=""$SOME_PREVIOUS_OUTPUT"" parameter_name=""some value with a &lt;!-- &apos;comment&apos; in it--&gt;""/>

Begin!

<goal>{{$input}}</goal>
");

var goal = "Write a poem about John Doe, then translate it into Chinese.";
var relevantFunctionsManual = await kernel.Functions.GetFunctionsManualAsync(new SequentialPlannerConfig(), goal, null);

ContextVariables vars = new(goal)
{
    ["available_functions"] = relevantFunctionsManual
};

var planResult = await kernel.RunAsync(semanticFunction, vars);
string? planResultString = planResult.GetValue<string>()?.Trim();
var xmlString = planResultString;
XmlDocument xmlDoc = new();

try
{
    xmlDoc.LoadXml("<xml>" + xmlString + "</xml>");
}
catch (XmlException e)
{
}

XmlNodeList solution = xmlDoc.GetElementsByTagName("plan");

var plan = new Plan(goal);

Console.Read();