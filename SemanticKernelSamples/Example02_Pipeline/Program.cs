using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.Skills.Core;

IKernel kernel = new KernelBuilder().Build();

// 加载技能
var text = kernel.ImportSkill(new TextSkill());

SKContext result = await kernel.RunAsync("    i n f i n i t e     s p a c e     ",
    text["TrimStart"],
    text["TrimEnd"],
    text["Uppercase"]);

Console.WriteLine(result);