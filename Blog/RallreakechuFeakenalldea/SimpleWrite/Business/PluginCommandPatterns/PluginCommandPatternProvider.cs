using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SimpleWrite.Business.TextEditors.CommandPatterns;

namespace SimpleWrite.Business.PluginCommandPatterns;

/// <summary>
/// 插件式的命令提供器
/// </summary>
internal class PluginCommandPatternProvider
{
    public void AddPatterns(CommandPatternManager commandPatternManager)
    {
        var manager = commandPatternManager;

        // 添加插件提供器
    }
}
