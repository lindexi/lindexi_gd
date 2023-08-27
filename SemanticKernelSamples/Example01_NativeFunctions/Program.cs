using Microsoft.SemanticKernel.Skills.Core;

// 创建技能
var text = new TextSkill();

// 直接调用技能里的方法
var result = text.Uppercase("ciao");

Console.WriteLine(result);
