using SimpleWrite.Business.TextEditors.CommandPatterns;

namespace SimpleWrite.ViewModels;

/// <summary>
/// 表示命令模式与匹配结果的数据模型。
/// </summary>
/// <param name="Pattern">原始命令模式。</param>
/// <param name="IsMatch">是否匹配当前文本。</param>
public sealed record CommandPaletteItem(ICommandPattern Pattern, bool IsMatch)
{
    /// <summary>
    /// 获取命令模式的标题。
    /// </summary>
    public string Title => Pattern.Title;
}
