using System;
using SimpleWrite.Business.TextEditors.CommandPatterns;

namespace SimpleWrite.Business.PluginCommandPatterns;

/// <summary>
/// 插件式的命令提供器
/// </summary>
internal class PluginCommandPatternProvider
{
    public void AddPatterns(CommandPatternManager commandPatternManager)
    {
        ArgumentNullException.ThrowIfNull(commandPatternManager);

        // 添加插件提供器
        commandPatternManager.AddCommandPattern(new RunCommandLineCommandPattern());
        commandPatternManager.AddCommandPattern(new OpenUrlCommandPattern());
    }
}