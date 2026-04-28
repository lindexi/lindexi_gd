using System;
using System.Collections.Generic;

using AvaloniaAgentLib.Model;
using AvaloniaAgentLib.ViewModel;

using SimpleWrite.Business.AgentConnectors.CopilotAbilityLoaders;
using SimpleWrite.Business.CopilotCommandPatterns;
using SimpleWrite.Business.SimpleWriteConfigurations;
using SimpleWrite.Business.TextEditors.CommandPatterns;

namespace SimpleWrite.Business.AgentConnectors;

sealed class CopilotPatternProvider(CopilotViewModel copilotViewModel, ConfigurationManager configurationManager)
{
    public void AddCopilotPatterns(CommandPatternManager commandPatternManager)
    {
        ArgumentNullException.ThrowIfNull(commandPatternManager);

        commandPatternManager.AddCommandPattern(new PolishSelectedTextCommandPattern(copilotViewModel));

        commandPatternManager.AddCommandPattern("发送内容到 Copilot 聊天", text => copilotViewModel.SendMessageInNewSessionAsync(text), priority: 200);

        commandPatternManager.AddCommandPattern("翻译为计算机英文", text =>
        {
            var prompt =
                $"""
                 请帮我将以下内容转述为地道的计算机英文，我将在即时聊天中使用：
                 {text}
                 """;
            return copilotViewModel.SendMessageInNewSessionAsync(prompt);
        }, priority: 180);

        commandPatternManager.AddCommandPattern("Json转C#类", text =>
        {
            var prompt =
                $"""
                 将以下 json 转换为 C# 的类型，要求使用 System.Text.Json 作为 Json 特性定义。要求 C# 属性命名符合 .NET 规范，采用帕斯卡风格：
                 {text}
                 """;
            return copilotViewModel.SendMessageInNewSessionAsync(prompt);
        }, supportSingleLine: false, priority: 160);

        AddXmlAbilityPatterns(commandPatternManager);
    }

    private void AddXmlAbilityPatterns(CommandPatternManager commandPatternManager)
    {
        var loadErrorList = new List<string>();
        foreach (var ability in CopilotAbilityLoader.Load(configurationManager, loadErrorList))
        {
            commandPatternManager.AddCommandPattern(ability.Title, text =>
            {
                string prompt = ability.CreatePrompt(text);
                return copilotViewModel.SendMessageInNewSessionAsync(prompt);
            }, supportSingleLine: ability.SupportSingleLine, priority: ability.Priority);
        }

        if (loadErrorList.Count > 0)
        {
            string message = "以下 Copilot 能力文件未成功加载：" + Environment.NewLine + string.Join(Environment.NewLine, loadErrorList);
            copilotViewModel.ChatMessages.Add(CopilotChatMessage.CreateAssistant(message, isPresetInfo: true));
        }
    }
}