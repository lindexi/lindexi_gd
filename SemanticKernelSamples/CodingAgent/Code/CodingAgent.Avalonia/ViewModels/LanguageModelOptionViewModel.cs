using System;

using AgentLib.Core.AgentApiManagers.LanguageModelProviders;

namespace CodingChatRoom.AvaloniaShell.ViewModels;

/// <summary>
/// 表示聊天界面中可选择的语言模型。
/// </summary>
public sealed class LanguageModelOptionViewModel
{
    internal LanguageModelOptionViewModel(ILanguageModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        Model = model;
        string provider = model.ModelDefinition.Provider;
        string modelName = model.ModelDefinition.ModelName;
        DisplayName = string.IsNullOrWhiteSpace(provider) ? modelName : $"{provider}/{modelName}";
    }

    /// <summary>
    /// 获取用于界面展示的模型名称。
    /// </summary>
    public string DisplayName { get; }

    internal ILanguageModel Model { get; }
}
