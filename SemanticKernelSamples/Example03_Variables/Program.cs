using System.ComponentModel;
using System.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.SkillDefinition;

var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Debug);
});
var logger = loggerFactory.CreateLogger("SemanticKernel");

IKernel kernel = new KernelBuilder().WithLogger(logger).Build();
var text = kernel.ImportSkill(new StaticTextSkill(), "text");

var variables = new ContextVariables("今天是: ");
variables.Set("day", DateTime.Now.ToString(CultureInfo.CurrentCulture));

SKContext result = await kernel.RunAsync(variables,
    text["AppendDay"],
    text["Uppercase"]);

Console.WriteLine(result);

class StaticTextSkill
{
    [SKFunction, Description("将所有的文本字符串修改为大写")]
    public static string Uppercase([Description("准备修改为大写的文本")] string input) =>
        input.ToUpperInvariant();

    [SKFunction, Description("追加 day 变量到字符串")]
    public static string AppendDay
    (
        [Description("准备被追加的文本")] string input,
        [Description("追加到文本后面的字符串")]
        string day
    )
        => input + day;
}